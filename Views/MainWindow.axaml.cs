using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Ktpt.CourseOrder.Services;

namespace Ktpt.CourseOrder.Views;

public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; } = null!;

    // Таймер, который прячет сообщение
    private readonly DispatcherTimer toastTimer = new() { Interval = TimeSpan.FromSeconds(4) };

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;

        toastTimer.Tick += (s, e) => HideToast();

        // При запуске открываем экран входа
        OpenPage(new LoginView());
    }

    public void OpenPage(UserControl page)
    {
        PageHost.Content = page;
        HideToast();

        // На экране входа меню не нужно
        Sidebar.IsVisible = page is not LoginView;

        // Шапка пользователя
        UserText.Text = Session.IsGuest ? "Гость" : Session.CurrentUser!.FullName;
        RoleText.Text = Session.IsGuest ? "только просмотр каталога" : Session.RoleName;
        AvatarText.Text = Session.IsGuest ? "?" : Session.CurrentUser!.Initials;
        LogoutText.Text = Session.IsGuest ? "Войти" : "Сменить пользователя";

        // Пункты меню по правам
        LoansButton.IsVisible = Session.IsStaff;
        UsersButton.IsVisible = Session.IsStaff;
        ProfileButton.IsVisible = !Session.IsGuest;
        // Принимать книги в фонд может библиотекарь, а добавлять читателей и книги — только администратор
        ManagePanel.IsVisible = Session.IsStaff;
        AddReaderButton.IsVisible = Session.IsAdmin;
        AddBookButton.IsVisible = Session.IsAdmin;

        // Подсвечиваем текущий раздел
        bool ownProfile = page is UserDetailsView details && details.UserId == Session.CurrentUser?.UserId;
        CatalogButton.Classes.Set("active", page is CatalogView or BookDetailsView);
        LoansButton.Classes.Set("active", page is LoansView);
        UsersButton.Classes.Set("active", page is UsersView || (page is UserDetailsView && !ownProfile));
        ProfileButton.Classes.Set("active", ownProfile);
        AddReaderButton.Classes.Set("active", page is AddUserView);
        AddBookButton.Classes.Set("active", page is AddBookView);
        AddCopiesButton.Classes.Set("active", page is AddCopyView);
    }

    public void ShowMessage(string text) => ShowToast(text, isError: false);

    public void ShowError(Exception error) => ShowToast(UiSupport.ErrorText(error), isError: true);

    private void ShowToast(string text, bool isError)
    {
        MessageText.Text = text;
        Toast.Classes.Set("error", isError);
        Toast.IsVisible = true;

        // Ошибки висят дольше, чтобы их успели прочитать
        toastTimer.Interval = TimeSpan.FromSeconds(isError ? 7 : 4);
        toastTimer.Stop();
        toastTimer.Start();
    }

    private void HideToast()
    {
        toastTimer.Stop();
        Toast.IsVisible = false;
    }

    private void ToastPressed(object? sender, PointerPressedEventArgs e) => HideToast();

    private void CatalogClick(object? sender, RoutedEventArgs e) => OpenPage(new CatalogView());
    private void LoansClick(object? sender, RoutedEventArgs e) => OpenPage(new LoansView());
    private void UsersClick(object? sender, RoutedEventArgs e) => OpenPage(new UsersView());
    private void AddReaderClick(object? sender, RoutedEventArgs e) => OpenPage(new AddUserView());
    private void AddBookClick(object? sender, RoutedEventArgs e) => OpenPage(new AddBookView());
    private void AddCopiesClick(object? sender, RoutedEventArgs e) => OpenPage(new AddCopyView());

    private void ProfileClick(object? sender, RoutedEventArgs e)
    {
        if (Session.CurrentUser != null)
            OpenPage(new UserDetailsView { UserId = Session.CurrentUser.UserId });
    }

    private void LogoutClick(object? sender, RoutedEventArgs e)
    {
        Session.Logout();
        OpenPage(new LoginView()); // Возвращаемся на экран входа
    }
}
