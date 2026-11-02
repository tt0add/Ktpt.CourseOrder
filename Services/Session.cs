using Ktpt.CourseOrder.Data;
using Ktpt.CourseOrder.Models;
using Microsoft.EntityFrameworkCore;

namespace Ktpt.CourseOrder.Services;

public static class Session
{
    // Текущий пользователь (null = гость)
    public static User? CurrentUser { get; private set; }

    // Название роли (Читатель, Библиотекарь, Администратор)
    public static string RoleName => CurrentUser?.Role?.Name ?? "";

    // Уровни доступа (адаптируйте названия под вашу таблицу roles)
    public static bool IsGuest => CurrentUser == null;
    public static bool IsStaff => RoleName == "Библиотекарь" || RoleName == "Администратор";
    public static bool IsAdmin => RoleName == "Администратор";

    // Подпись в шапке окна
    public static string Caption => CurrentUser == null ? "Гость" : $"{CurrentUser.FullName} · {RoleName}";

    // Загрузка списка пользователей для входа
    public static async Task<List<User>> GetUsersAsync()
    {
        using var db = Db.Create();
        return await db.Users
            .Include(u => u.Role)
            .OrderBy(u => u.LastName)
            .AsNoTracking()
            .ToListAsync();
    }

    // Вход в систему. Пароль проверяем по свежим данным из БД.
    // Если у пользователя пароль не задан — входит без пароля.
    public static async Task LoginAsync(int userId, string password)
    {
        using var db = Db.Create();
        User? user = await db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.UserId == userId);

        if (user == null) throw new AppException("Пользователь не найден. Обновите список.");
        if (!string.IsNullOrEmpty(user.AdminPassword) && user.AdminPassword != password)
            throw new AppException("Неверный пароль. Попробуйте снова.");

        CurrentUser = user;
    }

    // Выход
    public static void Logout() => CurrentUser = null;

    // Проверки прав
    public static void RequireStaff()
    {
        if (!IsStaff) throw new AppException("Недостаточно прав: нужна роль библиотекаря или администратора.");
    }

    public static void RequireAdmin()
    {
        if (!IsAdmin) throw new AppException("Недостаточно прав: нужна роль администратора.");
    }

    // Свои данные видит любой вошедший, чужие — только сотрудник
    public static void RequireSelfOrStaff(int userId)
    {
        if (CurrentUser == null) throw new AppException("Войдите в систему.");
        if (!IsStaff && CurrentUser.UserId != userId)
            throw new AppException("Недостаточно прав: можно смотреть только свой профиль.");
    }
}