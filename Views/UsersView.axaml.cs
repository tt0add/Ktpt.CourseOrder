using Avalonia.Controls;
using Avalonia.Interactivity;
using Ktpt.CourseOrder.Models;
using Ktpt.CourseOrder.Services;

namespace Ktpt.CourseOrder.Views;

public partial class UsersView : UserControl
{
    private const string AllRoles = "Все роли";

    private List<User> allUsers = new();

    public UsersView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            // Внутри проверяется, что зашёл сотрудник
            allUsers = await UserService.GetUsersAsync();

            var roles = allUsers.Select(u => u.Role.Name).Distinct().OrderBy(r => r).ToList();
            roles.Insert(0, AllRoles);
            RoleBox.ItemsSource = roles;
            RoleBox.SelectedIndex = 0;

            ApplyFilters();
        }
        catch (Exception ex)
        {
            CountText.Text = "Список недоступен";
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void FiltersChanged(object? sender, RoutedEventArgs e) => ApplyFilters();

    // Поиск по списку
    private void ApplyFilters()
    {
        if (UsersList == null) return;

        string text = (SearchBox.Text ?? "").Trim();
        string? role = RoleBox.SelectedItem as string;

        var filtered = allUsers.Where(u =>
            (text == "" ||
             u.FullName.Contains(text, StringComparison.OrdinalIgnoreCase) ||
             u.Email.Contains(text, StringComparison.OrdinalIgnoreCase) ||
             u.GroupText.Contains(text, StringComparison.OrdinalIgnoreCase)) &&
            (role == null || role == AllRoles || u.Role.Name == role)
        ).ToList();

        UsersList.ItemsSource = filtered;
        CountText.Text = $"Показано {filtered.Count} из {allUsers.Count} · с просрочкой: {allUsers.Count(u => u.HasOverdue)}";
    }

    // Переход в профиль
    private void UserClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is User user)
            MainWindow.Instance.OpenPage(new UserDetailsView { UserId = user.UserId });
    }
}
