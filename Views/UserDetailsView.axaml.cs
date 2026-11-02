using Avalonia.Controls;
using Avalonia.Interactivity;
using Ktpt.CourseOrder.Models;
using Ktpt.CourseOrder.Services;

namespace Ktpt.CourseOrder.Views;

public partial class UserDetailsView : UserControl
{
    public int UserId { get; set; }

    // Последние загруженные данные — для заполнения формы редактирования
    private User? loadedUser;

    public UserDetailsView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadUserDataAsync();
    }

    private async Task LoadUserDataAsync()
    {
        try
        {
            // 1. Пользователь (внутри проверка прав: свой профиль или сотрудник)
            User user = await UserService.GetUserAsync(UserId);

            AvatarText.Text = user.Initials;
            HeaderText.Text = user.FullName;
            RoleText.Text = user.Role?.Name ?? "Неизвестно";
            EmailText.Text = user.Email;
            PhoneText.Text = user.PhoneText;
            GroupText.Text = user.GroupText;

            // Кнопки сотрудника
            BackButton.IsVisible = Session.IsStaff;
            IssueButton.IsVisible = Session.IsStaff && !IssuePanel.IsVisible;
            EditButton.IsVisible = Session.IsAdmin && !EditPanel.IsVisible;
            loadedUser = user;

            // 2. История выдач
            List<Loan> loans = await LoanService.GetUserLoansAsync(UserId);
            LoansList.ItemsSource = loans;
            EmptyText.IsVisible = loans.Count == 0;

            int overdue = loans.Count(l => l.IsOverdue);
            ActiveCountText.Text = loans.Count(l => l.IsActive).ToString();
            OverdueCountText.Text = overdue.ToString();
            TotalCountText.Text = loans.Count.ToString();
            OverdueBadge.IsVisible = overdue > 0;
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Кнопку «Принять» показываем только сотруднику и только для книг на руках
    private void ReturnButtonLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button)
            button.IsVisible = Session.IsStaff && button.Tag is true;
    }

    // Открытие панели выдачи
    private async void IssueClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            AvailableCopiesBox.ItemsSource = await LoanService.GetFreeCopiesAsync();
            DueHintText.Text = $"Срок возврата: {LoanRules.CalculateDueDate(DateTime.Today):dd.MM.yyyy} ({LoanRules.LoanDays} дней). " +
                               $"Не больше {LoanRules.MaxActiveLoans} книг на руках, при просрочке выдача запрещена.";

            IssuePanel.IsVisible = true;
            IssueButton.IsVisible = false;
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void CancelIssueClick(object? sender, RoutedEventArgs e)
    {
        IssuePanel.IsVisible = false;
        IssueButton.IsVisible = true;
    }

    // Подтверждение выдачи (сохранение в БД)
    private async void ConfirmIssueClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (AvailableCopiesBox.SelectedItem is not Copy copy)
                throw new AppException("Выберите книгу для выдачи.");

            // Все проверки (свободен ли экземпляр, лимиты, просрочки) — в LoanService
            await LoanService.CreateLoanAsync(copy.CopyId, UserId);

            IssuePanel.IsVisible = false;
            MainWindow.Instance.ShowMessage($"Книга «{copy.Book?.Title}» выдана");

            // Перезагружаем страницу, чтобы история обновилась
            await LoadUserDataAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private async void ReturnClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not Loan loan) return;

        try
        {
            await LoanService.ReturnBookAsync(loan.LoanId);
            MainWindow.Instance.ShowMessage($"«{loan.BookTitle}» возвращена в библиотеку");
            await LoadUserDataAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Открытие панели редактирования (только администратор)
    private async void EditClick(object? sender, RoutedEventArgs e)
    {
        if (loadedUser == null) return;
        try
        {
            var roles = await UserService.GetRolesAsync();
            EditRoleBox.ItemsSource = roles;
            EditRoleBox.SelectedItem = roles.FirstOrDefault(r => r.RoleId == loadedUser.RoleId);

            EditLastNameBox.Text = loadedUser.LastName;
            EditFirstNameBox.Text = loadedUser.FirstName;
            EditMiddleNameBox.Text = loadedUser.MiddleName;
            EditEmailBox.Text = loadedUser.Email;
            EditPhoneBox.Text = loadedUser.Phone;
            EditGroupBox.Text = loadedUser.StudentGroup;

            EditPanel.IsVisible = true;
            EditButton.IsVisible = false;
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void CancelEditClick(object? sender, RoutedEventArgs e)
    {
        EditPanel.IsVisible = false;
        EditButton.IsVisible = true;
    }

    private async void SaveEditClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (EditRoleBox.SelectedItem is not Role role) throw new AppException("Выберите роль.");

            var changed = new User
            {
                UserId = UserId,
                LastName = EditLastNameBox.Text ?? "",
                FirstName = EditFirstNameBox.Text ?? "",
                MiddleName = EmptyToNull(EditMiddleNameBox.Text),
                Email = EditEmailBox.Text ?? "",
                Phone = EmptyToNull(EditPhoneBox.Text),
                StudentGroup = EmptyToNull(EditGroupBox.Text),
                RoleId = role.RoleId
            };

            await UserService.UpdateUserAsync(changed);

            EditPanel.IsVisible = false;
            MainWindow.Instance.ShowMessage("Данные пользователя сохранены");
            await LoadUserDataAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private static string? EmptyToNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private void BackClick(object? sender, RoutedEventArgs e)
    {
        MainWindow.Instance.OpenPage(new UsersView());
    }
}
