namespace Ktpt.CourseOrder.Services;

public static class LoanRules
{
    // Срок выдачи книги в днях
    public static int LoanDays => 14;

    // Сколько книг одновременно может быть на руках у читателя
    public static int MaxActiveLoans => 5;

    // Названия статусов в таблице loan_statuses.
    // Ищем статус по названию, а не по id: id в базе начинаются с 0, и на них полагаться опасно.
    public const string StatusIssued = "Выдана";
    public const string StatusReturned = "Возвращена";
    public const string StatusOverdue = "Просрочена";

    // Вычисление даты возврата
    public static DateTime CalculateDueDate(DateTime loanDate)
    {
        return loanDate.Date.AddDays(LoanDays);
    }

    // Проверка, просрочена ли книга
    public static bool IsOverdue(DateTime dueDate, DateTime? returnDate)
    {
        if (returnDate.HasValue) return false; // уже вернули
        return DateTime.Today > dueDate.Date;
    }
}
