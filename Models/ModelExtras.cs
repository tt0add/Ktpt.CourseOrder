using System;
using Ktpt.CourseOrder.Services;

namespace Ktpt.CourseOrder.Models
{
    // Дополнительные свойства для интерфейса. Свойства только с get EF Core не сохраняет в БД.

    public partial class User
    {
        // ФИО одной строкой. Trim убирает пробел в конце, если отчества нет.
        public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();

        // Буквы для круглой «аватарки»
        public string Initials => $"{FirstLetter(LastName)}{FirstLetter(FirstName)}";

        public string GroupText => string.IsNullOrWhiteSpace(StudentGroup) ? "—" : StudentGroup;
        public string PhoneText => string.IsNullOrWhiteSpace(Phone) ? "Не указан" : Phone;

        // Сколько книг на руках (считается по загруженным выдачам)
        public int ActiveLoansCount => Loans.Count(l => l.ReturnDate == null);
        public bool HasOverdue => Loans.Any(l => l.IsOverdue);

        public string LoansText => ActiveLoansCount == 0 ? "Нет книг" : $"На руках: {ActiveLoansCount}";

        // ToString решает, как объект выглядит в ComboBox и ListBox.
        public override string ToString() => $"{FullName} — {Email}";

        private static string FirstLetter(string? s) => string.IsNullOrEmpty(s) ? "" : s[..1].ToUpper();
    }

    public partial class Author
    {
        // «Пушкин А. С.»
        public string ShortName
        {
            get
            {
                string initials = string.IsNullOrEmpty(FirstName) ? "" : $" {FirstName[0]}.";
                if (!string.IsNullOrEmpty(MiddleName)) initials += $" {MiddleName[0]}.";
                return LastName + initials;
            }
        }

        public override string ToString() => $"{LastName} {FirstName} {MiddleName}".Trim();
    }

    public partial class Role
    {
        public override string ToString() => Name;
    }

    public partial class Publisher
    {
        public override string ToString() => string.IsNullOrWhiteSpace(City) ? Name : $"{Name} ({City})";
    }

    public partial class Book
    {
        public override string ToString() => PublishYear == null ? Title : $"{Title}, {PublishYear}";
    }

    public partial class Copy
    {
        // Так экземпляр будет выглядеть в выпадающем списке
        public override string ToString()
        {
            // Берем название книги и добавляем полку (если книга подгружена из БД)
            string title = Book?.Title ?? "Неизвестная книга";
            return $"{title} · экз. №{CopyId} (полка: {LocationShelf ?? "—"})";
        }
    }

    public partial class Loan
    {
        public bool IsActive => ReturnDate == null;
        public bool IsReturned => ReturnDate != null;
        public bool IsOverdue => LoanRules.IsOverdue(DueDate, ReturnDate);
        public bool IsOnHand => IsActive && !IsOverdue;

        public string BookTitle => Copy?.Book?.Title ?? "—";
        public string ReaderName => User?.FullName ?? "—";
        public string CopyText => $"Экз. №{CopyId} · полка {Copy?.LocationShelf ?? "—"}";

        // Статус считаем по датам — так просрочка видна сразу, без отдельного обновления БД
        public string StatusText => IsReturned ? LoanRules.StatusReturned
            : IsOverdue ? LoanRules.StatusOverdue
            : "На руках";

        public string PeriodText => $"{LoanDate:dd.MM.yyyy} → {DueDate:dd.MM.yyyy}";

        public string TermText
        {
            get
            {
                if (ReturnDate is DateTime returned) return $"Возвращена {returned:dd.MM.yyyy}";
                int days = (DueDate.Date - DateTime.Today).Days;
                if (days < 0) return $"Просрочка {-days} дн.";
                if (days == 0) return "Вернуть сегодня";
                return $"Осталось {days} дн.";
            }
        }
    }
}
