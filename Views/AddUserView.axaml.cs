using Avalonia.Controls;
using Avalonia.Interactivity;
using Ktpt.CourseOrder.Models;
using Ktpt.CourseOrder.Services;

namespace Ktpt.CourseOrder.Views;

public partial class AddUserView : UserControl
{
    public AddUserView()
    {
        InitializeComponent();
    }

    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var reader = new User
            {
                LastName = LastNameBox.Text ?? "",
                FirstName = FirstNameBox.Text ?? "",
                MiddleName = EmptyToNull(MiddleNameBox.Text),
                Email = EmailBox.Text ?? "",
                Phone = EmptyToNull(PhoneBox.Text),
                StudentGroup = EmptyToNull(GroupBox.Text)
            };

            // Все проверки — в UserService
            int id = await UserService.AddReaderAsync(reader, PasswordBox.Text ?? "");

            MainWindow.Instance.OpenPage(new UserDetailsView { UserId = id });
            MainWindow.Instance.ShowMessage($"Читатель {reader.FullName} добавлен");
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void CancelClick(object? sender, RoutedEventArgs e) => MainWindow.Instance.OpenPage(new UsersView());

    private static string? EmptyToNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
