using Avalonia.Controls;
using Avalonia.Interactivity;
using Ktpt.CourseOrder.Models;
using Ktpt.CourseOrder.Services;

namespace Ktpt.CourseOrder.Views;

public partial class AddBookView : UserControl
{
    public AddBookView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadListsAsync();
    }

    private async Task LoadListsAsync()
    {
        try
        {
            AuthorBox.ItemsSource = await CatalogService.GetAuthorsAsync();
            PublisherBox.ItemsSource = await CatalogService.GetPublishersAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (AuthorBox.SelectedItem is not Author author) throw new AppException("Выберите автора.");
            if (PublisherBox.SelectedItem is not Publisher publisher) throw new AppException("Выберите издательство.");

            int? year = null;
            if (!string.IsNullOrWhiteSpace(YearBox.Text))
            {
                if (!int.TryParse(YearBox.Text.Trim(), out int y)) throw new AppException("Год издания — это число, например 2024.");
                year = y;
            }

            var book = new Book
            {
                Title = TitleBox.Text ?? "",
                PublisherId = publisher.PublisherId,
                Genre = EmptyToNull(GenreBox.Text),
                PublishYear = year,
                Isbn = EmptyToNull(IsbnBox.Text),
                ImagePath = EmptyToNull(ImageBox.Text)
            };

            int id = await CatalogService.AddBookAsync(book, author.AuthorId);

            MainWindow.Instance.OpenPage(new BookDetailsView { BookId = id });
            MainWindow.Instance.ShowMessage($"Книга «{book.Title}» добавлена в каталог");
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void CancelClick(object? sender, RoutedEventArgs e) => MainWindow.Instance.OpenPage(new CatalogView());

    private static string? EmptyToNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
