using Ktpt.CourseOrder.Data;
using Ktpt.CourseOrder.Models;
using Ktpt.CourseOrder.Views;
using Microsoft.EntityFrameworkCore;

namespace Ktpt.CourseOrder.Services;

public static class CatalogService
{
    public static async Task<List<BookCard>> LoadCardsAsync()
    {
        using var db = Db.Create();

        // Книги вместе с издательствами, авторами, экземплярами и их активными выдачами
        List<Book> books = await db.Books
            .Include(b => b.Publisher)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.Copies).ThenInclude(c => c.Loans.Where(l => l.ReturnDate == null))
            .OrderBy(b => b.Title)
            .AsNoTracking()
            .ToListAsync();

        return books.Select(ToCard).ToList();
    }

    // Подробности о книге: карточка + список экземпляров
    public static async Task<(BookCard Card, List<CopyRow> Copies)> LoadBookAsync(int bookId)
    {
        using var db = Db.Create();

        Book? book = await db.Books
            .Include(b => b.Publisher)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.Copies).ThenInclude(c => c.Loans.Where(l => l.ReturnDate == null))
                .ThenInclude(l => l.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BookId == bookId);

        if (book == null) throw new AppException("Книга не найдена.");

        var rows = book.Copies
            .OrderBy(c => c.CopyId)
            .Select(c =>
            {
                Loan? loan = c.Loans.FirstOrDefault();
                var row = new CopyRow
                {
                    CopyId = c.CopyId,
                    Shelf = string.IsNullOrWhiteSpace(c.LocationShelf) ? "—" : c.LocationShelf,
                    IsFree = loan == null,
                    IsOverdue = loan?.IsOverdue ?? false,
                    StatusText = loan == null ? "Свободен" : loan.IsOverdue ? "Просрочен" : "Выдан",
                };

                // Кому выдана книга, видят только сотрудники
                if (loan != null)
                    row.DetailsText = Session.IsStaff
                        ? $"{loan.User.FullName} · до {loan.DueDate:dd.MM.yyyy}"
                        : $"Ожидается к {loan.DueDate:dd.MM.yyyy}";

                return row;
            })
            .ToList();

        return (ToCard(book), rows);
    }

    // Справочники для формы добавления книги
    public static async Task<List<Author>> GetAuthorsAsync()
    {
        using var db = Db.Create();
        return await db.Authors.OrderBy(a => a.LastName).AsNoTracking().ToListAsync();
    }

    public static async Task<List<Publisher>> GetPublishersAsync()
    {
        using var db = Db.Create();
        return await db.Publishers.OrderBy(p => p.Name).AsNoTracking().ToListAsync();
    }

    public static async Task<List<Book>> GetBooksAsync()
    {
        using var db = Db.Create();
        return await db.Books.OrderBy(b => b.Title).AsNoTracking().ToListAsync();
    }

    // Добавление книги в каталог (только администратор)
    public static async Task<int> AddBookAsync(Book book, int authorId)
    {
        Session.RequireAdmin();

        book.Title = book.Title.Trim();
        if (book.Title == "") throw new AppException("Укажите название книги.");
        if (book.PublishYear is int year && (year < 1400 || year > DateTime.Today.Year))
            throw new AppException($"Год издания должен быть от 1400 до {DateTime.Today.Year}.");

        using var db = Db.Create();

        if (!await db.Publishers.AnyAsync(p => p.PublisherId == book.PublisherId))
            throw new AppException("Выберите издательство.");
        if (!await db.Authors.AnyAsync(a => a.AuthorId == authorId))
            throw new AppException("Выберите автора.");

        book.BookAuthors.Add(new BookAuthor { AuthorId = authorId });
        db.Books.Add(book);
        await db.SaveChangesAsync();
        return book.BookId;
    }

    // Приём экземпляров в фонд (только сотрудники)
    public static async Task AddCopiesAsync(int bookId, string shelf, int count)
    {
        Session.RequireStaff();

        shelf = shelf.Trim();
        if (shelf == "") throw new AppException("Укажите полку, например A-1.");
        if (count < 1 || count > 100) throw new AppException("Количество экземпляров — от 1 до 100.");

        using var db = Db.Create();
        if (!await db.Books.AnyAsync(b => b.BookId == bookId)) throw new AppException("Выберите книгу.");

        for (int i = 0; i < count; i++)
            db.Copies.Add(new Copy { BookId = bookId, LocationShelf = shelf });

        await db.SaveChangesAsync();
    }

    private static BookCard ToCard(Book b)
    {
        string authors = string.Join(", ", b.BookAuthors.Select(ba => ba.Author.ShortName));

        return new BookCard
        {
            Id = b.BookId,
            Title = b.Title,
            Authors = string.IsNullOrEmpty(authors) ? "Автор не указан" : authors,
            Publisher = b.Publisher?.Name ?? "Неизвестно",
            Genre = string.IsNullOrWhiteSpace(b.Genre) ? "Без жанра" : b.Genre,
            YearText = b.PublishYear?.ToString() ?? "",
            Isbn = string.IsNullOrWhiteSpace(b.Isbn) ? "—" : b.Isbn,
            TotalCopies = b.Copies.Count,
            // Свободен экземпляр, у которого нет активной выдачи
            AvailableCopies = b.Copies.Count(c => c.Loans.Count == 0),
            // Картинка (если ImagePath равен null, UiSupport покажет заглушку)
            Image = UiSupport.Picture(b.ImagePath),
            HasImage = UiSupport.HasPicture(b.ImagePath)
        };
    }
}
