using System.Data;
using Ktpt.CourseOrder.Data;
using Ktpt.CourseOrder.Models;
using Microsoft.EntityFrameworkCore;

namespace Ktpt.CourseOrder.Services;

public static class LoanService
{
    // 1. Оформление выдачи (только сотрудники)
    public static async Task<int> CreateLoanAsync(int copyId, int userId)
    {
        Session.RequireStaff();

        using var db = Db.Create();
        // Serializable — чтобы два библиотекаря не выдали один экземпляр одновременно
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        if (!await db.Copies.AnyAsync(c => c.CopyId == copyId))
            throw new AppException("Экземпляр не найден.");
        if (!await db.Users.AnyAsync(u => u.UserId == userId))
            throw new AppException("Читатель не найден.");

        // Проверяем, свободен ли экземпляр (нет активной выдачи)
        bool isBusy = await db.Loans.AnyAsync(l => l.CopyId == copyId && l.ReturnDate == null);
        if (isBusy) throw new AppException("Этот экземпляр уже выдан другому читателю.");

        // Проверяем, не нарушает ли читатель правила
        var activeLoans = await db.Loans
            .Where(l => l.UserId == userId && l.ReturnDate == null)
            .Select(l => l.DueDate)
            .ToListAsync();

        if (activeLoans.Any(due => LoanRules.IsOverdue(due, null)))
            throw new AppException("У читателя есть просроченные книги. Сначала оформите возврат.");
        if (activeLoans.Count >= LoanRules.MaxActiveLoans)
            throw new AppException($"У читателя уже {activeLoans.Count} книг на руках — это максимум.");

        var loan = new Loan
        {
            CopyId = copyId,
            UserId = userId,
            StatusId = await StatusIdAsync(db, LoanRules.StatusIssued),
            LoanDate = DateTime.Now,
            DueDate = LoanRules.CalculateDueDate(DateTime.Today),
            ReturnDate = null
        };

        db.Loans.Add(loan);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return loan.LoanId;
    }

    // 2. Возврат книги
    public static async Task ReturnBookAsync(int loanId)
    {
        Session.RequireStaff();
        using var db = Db.Create();

        Loan? loan = await db.Loans.FindAsync(loanId);
        if (loan == null) throw new AppException("Выдача не найдена.");
        if (loan.ReturnDate != null) throw new AppException("Книга уже возвращена.");

        loan.ReturnDate = DateTime.Now;
        loan.StatusId = await StatusIdAsync(db, LoanRules.StatusReturned);

        await db.SaveChangesAsync();
    }

    // 3. Все выдачи (для библиотекаря)
    public static async Task<List<Loan>> GetLoansAsync()
    {
        Session.RequireStaff();
        using var db = Db.Create();

        return await db.Loans
            .Include(l => l.User)
            .Include(l => l.Copy).ThenInclude(c => c.Book)
            .Include(l => l.Status)
            .OrderByDescending(l => l.LoanDate)
            .AsNoTracking()
            .ToListAsync();
    }

    // 4. Выдачи одного читателя (сотрудник — любого, читатель — только свои)
    public static async Task<List<Loan>> GetUserLoansAsync(int userId)
    {
        Session.RequireSelfOrStaff(userId);
        using var db = Db.Create();

        return await db.Loans
            .Include(l => l.Copy).ThenInclude(c => c.Book)
            .Include(l => l.Status)
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.ReturnDate != null) // сначала то, что на руках
            .ThenByDescending(l => l.LoanDate)
            .AsNoTracking()
            .ToListAsync();
    }

    // 5. Свободные экземпляры (можно ограничить одной книгой)
    public static async Task<List<Copy>> GetFreeCopiesAsync(int? bookId = null)
    {
        Session.RequireStaff();
        using var db = Db.Create();

        return await db.Copies
            .Include(c => c.Book)
            .Where(c => bookId == null || c.BookId == bookId)
            .Where(c => !c.Loans.Any(l => l.ReturnDate == null))
            .OrderBy(c => c.Book.Title).ThenBy(c => c.CopyId)
            .AsNoTracking()
            .ToListAsync();
    }

    private static async Task<int> StatusIdAsync(CollegeContext db, string name)
    {
        int? id = await db.LoanStatuses
            .Where(s => s.StatusName == name)
            .Select(s => (int?)s.StatusId)
            .FirstOrDefaultAsync();

        return id ?? throw new AppException($"В таблице loan_statuses нет статуса «{name}».");
    }
}
