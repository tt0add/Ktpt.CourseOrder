using Avalonia.Controls;
using Avalonia.Interactivity;
using Ktpt.CourseOrder.Models;
using Ktpt.CourseOrder.Services;

namespace Ktpt.CourseOrder.Views;

public partial class LoansView : UserControl
{
    private List<Loan> allLoans = new();

    public LoansView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadLoansAsync();
    }

    private async Task LoadLoansAsync()
    {
        try
        {
            allLoans = await LoanService.GetLoansAsync();

            ActiveCountText.Text = allLoans.Count(l => l.IsOnHand).ToString();
            OverdueCountText.Text = allLoans.Count(l => l.IsOverdue).ToString();
            ReturnedCountText.Text = allLoans.Count(l => l.IsReturned).ToString();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void FiltersChanged(object? sender, RoutedEventArgs e) => ApplyFilters();

    private void ApplyFilters()
    {
        if (LoansList == null) return;

        string text = (SearchBox.Text ?? "").Trim();

        IEnumerable<Loan> query = FilterBox.SelectedIndex switch
        {
            1 => allLoans.Where(l => l.IsActive), // «На руках» — включая просроченные
            2 => allLoans.Where(l => l.IsOverdue),
            3 => allLoans.Where(l => l.IsReturned),
            _ => allLoans
        };

        if (text != "")
            query = query.Where(l =>
                l.BookTitle.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                l.ReaderName.Contains(text, StringComparison.OrdinalIgnoreCase));

        // Просроченные — наверх
        var list = query.OrderByDescending(l => l.IsOverdue).ThenByDescending(l => l.LoanDate).ToList();

        LoansList.ItemsSource = list;
        EmptyText.IsVisible = list.Count == 0;
    }

    private async void ReturnClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not Loan loan) return;

        try
        {
            await LoanService.ReturnBookAsync(loan.LoanId);
            MainWindow.Instance.ShowMessage($"«{loan.BookTitle}» возвращена в библиотеку");
            await LoadLoansAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void ReaderClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is Loan loan)
            MainWindow.Instance.OpenPage(new UserDetailsView { UserId = loan.UserId });
    }
}
