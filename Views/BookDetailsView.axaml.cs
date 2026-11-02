using Avalonia.Controls;
using Avalonia.Interactivity;
using Ktpt.CourseOrder.Models;
using Ktpt.CourseOrder.Services;

namespace Ktpt.CourseOrder.Views;

public partial class BookDetailsView : UserControl
{
    public int BookId { get; set; }

    public BookDetailsView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadBookAsync();
    }

    private async Task LoadBookAsync()
    {
        try
        {
            var (card, copies) = await CatalogService.LoadBookAsync(BookId);

            CoverImage.Source = card.Image;
            CoverImage.IsVisible = card.HasImage;
            GeneratedCover.IsVisible = !card.HasImage;
            CoverTitleText.Text = card.Title;
            CoverAuthorsText.Text = card.Authors;
            TitleText.Text = card.Title;
            AuthorsText.Text = card.Authors;
            GenreText.Text = card.Genre;
            PublisherText.Text = card.Publisher;
            YearText.Text = card.YearText == "" ? "—" : card.YearText;
            IsbnText.Text = card.Isbn;
            CopiesCountText.Text = card.TotalCopies.ToString();

            AvailabilityText.Text = card.CopiesText;
            AvailabilityBadge.Classes.Set("success", card.IsAvailable && !card.IsLow);
            AvailabilityBadge.Classes.Set("warning", card.IsLow);
            AvailabilityBadge.Classes.Set("danger", !card.IsAvailable);

            CopiesList.ItemsSource = copies;

            // Панель выдачи — только сотрудникам и только если есть свободные экземпляры
            IssuePanel.IsVisible = Session.IsStaff && card.IsAvailable;
            if (IssuePanel.IsVisible)
            {
                ReaderBox.ItemsSource = await UserService.GetUsersAsync();
                CopyBox.ItemsSource = await LoanService.GetFreeCopiesAsync(BookId);
                CopyBox.SelectedIndex = 0;
                DueHintText.Text = $"Срок возврата: {LoanRules.CalculateDueDate(DateTime.Today):dd.MM.yyyy} ({LoanRules.LoanDays} дней)";
            }
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private async void IssueClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (ReaderBox.SelectedItem is not User reader) throw new AppException("Выберите читателя.");
            if (CopyBox.SelectedItem is not Copy copy) throw new AppException("Выберите экземпляр.");

            await LoanService.CreateLoanAsync(copy.CopyId, reader.UserId);
            MainWindow.Instance.ShowMessage($"Экземпляр №{copy.CopyId} выдан: {reader.FullName}");

            // Обновляем страницу, чтобы экземпляр стал «Выдан»
            await LoadBookAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void BackClick(object? sender, RoutedEventArgs e) => MainWindow.Instance.OpenPage(new CatalogView());
}
