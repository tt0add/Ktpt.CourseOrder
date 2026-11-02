using Avalonia.Media.Imaging;

namespace Ktpt.CourseOrder.Services;

// Карточка книги для каталога
public class BookCard
{
    public int Id { get; set; } // book_id
    public string Title { get; set; } = ""; // Название
    public string Authors { get; set; } = ""; // Авторы (склеенные)
    public string Publisher { get; set; } = ""; // Издательство
    public string Genre { get; set; } = ""; // Жанр
    public string YearText { get; set; } = ""; // Год издания или пусто
    public string Isbn { get; set; } = ""; // ISBN или прочерк
    public int TotalCopies { get; set; } // Всего экземпляров
    public int AvailableCopies { get; set; } // Сколько свободно
    public Bitmap? Image { get; set; } // Обложка (или заглушка)
    public bool HasImage { get; set; } // false — рисуем обложку сами (название на цветном фоне)

    public bool IsAvailable => AvailableCopies > 0;
    public bool IsLow => IsAvailable && AvailableCopies <= 2;

    public string CopiesText => TotalCopies == 0 ? "Нет экземпляров"
        : AvailableCopies == 0 ? "Все экземпляры выданы"
        : $"Свободно {AvailableCopies} из {TotalCopies}";
}

// Строка списка экземпляров на странице книги
public class CopyRow
{
    public int CopyId { get; set; }
    public string Shelf { get; set; } = "";
    public bool IsFree { get; set; }
    public bool IsOverdue { get; set; }
    public string StatusText { get; set; } = "";
    public string DetailsText { get; set; } = "";
}
