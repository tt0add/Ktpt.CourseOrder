using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ktpt.CourseOrder.Models;
using Ktpt.CourseOrder.Services;

namespace Ktpt.CourseOrder.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            UsersBox.ItemsSource = await Session.GetUsersAsync();
        }
        catch (Exception ex)
        {
            ShowError(UiSupport.ErrorText(ex));
        }
    }

    private async void ReloadClick(object? sender, RoutedEventArgs e) => await LoadUsersAsync();

    // У кого пароль не задан — поле пароля не показываем
    private void UserChanged(object? sender, SelectionChangedEventArgs e)
    {
        User? user = UsersBox.SelectedItem as User;
        PasswordPanel.IsVisible = user == null || !string.IsNullOrEmpty(user.AdminPassword);
        PasswordBox.Text = "";
        ErrorText.IsVisible = false;
        if (PasswordPanel.IsVisible) PasswordBox.Focus();
    }

    // Enter в поле пароля = нажать «Войти»
    private void PasswordKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) LoginClick(sender, e);
    }

    private async void LoginClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            User? user = UsersBox.SelectedItem as User;
            if (user == null) throw new AppException("Выберите пользователя из списка.");

            // Пароль проверяется внутри Session.LoginAsync
            await Session.LoginAsync(user.UserId, PasswordBox.Text ?? "");

            // После входа открывается каталог книг
            MainWindow.Instance.OpenPage(new CatalogView());
            MainWindow.Instance.ShowMessage($"Добро пожаловать, {Session.CurrentUser!.FirstName}!");
        }
        catch (Exception ex)
        {
            ShowError(UiSupport.ErrorText(ex));
            PasswordBox.Text = ""; // Очищаем поле пароля при ошибке
        }
    }

    private void GuestClick(object? sender, RoutedEventArgs e)
    {
        Session.Logout();
        MainWindow.Instance.OpenPage(new CatalogView());
    }

    private void ShowError(string text)
    {
        ErrorText.Text = text;
        ErrorText.IsVisible = true;
    }
}
