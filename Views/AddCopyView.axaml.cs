using Avalonia.Controls;
using Avalonia.Interactivity;
using Ktpt.CourseOrder.Models;
using Ktpt.CourseOrder.Services;

namespace Ktpt.CourseOrder.Views;

public partial class AddCopyView : UserControl
{
    public AddCopyView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadBooksAsync();
    }

    private async Task LoadBooksAsync()
    {
        try
        {
            BookBox.ItemsSource = await CatalogService.GetBooksAsync();
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
            if (BookBox.SelectedItem is not Book book) throw new AppException("Выберите книгу.");
            int count = (int)(CountBox.Value ?? 1);

            await CatalogService.AddCopiesAsync(book.BookId, ShelfBox.Text ?? "", count);

            MainWindow.Instance.OpenPage(new BookDetailsView { BookId = book.BookId });
            MainWindow.Instance.ShowMessage($"«{book.Title}»: добавлено экземпляров — {count}");
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void CancelClick(object? sender, RoutedEventArgs e) => MainWindow.Instance.OpenPage(new CatalogView());
}
