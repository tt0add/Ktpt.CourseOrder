using Avalonia.Controls;
using Avalonia.Interactivity;
using Ktpt.CourseOrder.Services;

namespace Ktpt.CourseOrder.Views;

public partial class CatalogView : UserControl
{
    private const string AllGenres = "Все жанры";

    // Храним все загруженные книги, чтобы поиск фильтровал их, а не ходил каждый раз в БД
    private List<BookCard> allCards = new();

    public CatalogView()
    {
        InitializeComponent();

        // При загрузке страницы сразу идем в базу
        Loaded += async (s, e) => await LoadBooksAsync();
    }

    private async Task LoadBooksAsync()
    {
        try
        {
            allCards = await CatalogService.LoadCardsAsync();

            // Список жанров для фильтра
            var genres = allCards.Select(c => c.Genre).Distinct().OrderBy(g => g).ToList();
            genres.Insert(0, AllGenres);
            GenreBox.ItemsSource = genres;
            GenreBox.SelectedIndex = 0;

            ApplyFilters();
        }
        catch (Exception ex)
        {
            CountText.Text = "Не удалось загрузить каталог";
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Срабатывает при вводе текста, смене жанра и галочки
    private void FiltersChanged(object? sender, RoutedEventArgs e) => ApplyFilters();

    private void ApplyFilters()
    {
        // Событие может прийти раньше, чем XAML полностью создан
        if (BooksList == null) return;

        string text = (SearchBox.Text ?? "").Trim();
        string? genre = GenreBox.SelectedItem as string;
        bool availableOnly = AvailableOnlyBox.IsChecked == true;

        // Фильтруем без учета регистра
        var filtered = allCards.Where(c =>
            (text == "" ||
             c.Title.Contains(text, StringComparison.OrdinalIgnoreCase) ||
             c.Authors.Contains(text, StringComparison.OrdinalIgnoreCase) ||
             c.Publisher.Contains(text, StringComparison.OrdinalIgnoreCase)) &&
            (genre == null || genre == AllGenres || c.Genre == genre) &&
            (!availableOnly || c.IsAvailable)
        ).ToList();

        BooksList.ItemsSource = filtered;
        EmptyPanel.IsVisible = filtered.Count == 0;

        int free = allCards.Sum(c => c.AvailableCopies);
        CountText.Text = $"Показано {filtered.Count} из {allCards.Count} книг · свободно экземпляров: {free}";
    }

    private void BookClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is BookCard card)
            MainWindow.Instance.OpenPage(new BookDetailsView { BookId = card.Id });
    }
}
