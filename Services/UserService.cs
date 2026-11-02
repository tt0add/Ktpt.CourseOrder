using Ktpt.CourseOrder.Data;
using Ktpt.CourseOrder.Models;
using Microsoft.EntityFrameworkCore;

namespace Ktpt.CourseOrder.Services;

public static class UserService
{
    // Список всех пользователей вместе с книгами на руках (для сотрудников)
    public static async Task<List<User>> GetUsersAsync()
    {
        Session.RequireStaff();
        using var db = Db.Create();

        return await db.Users
            .Include(u => u.Role)
            .Include(u => u.Loans.Where(l => l.ReturnDate == null))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .AsNoTracking()
            .ToListAsync();
    }

    // Один пользователь (сотрудник — любой, читатель — только он сам)
    public static async Task<User> GetUserAsync(int userId)
    {
        Session.RequireSelfOrStaff(userId);
        using var db = Db.Create();

        User? user = await db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);

        return user ?? throw new AppException("Пользователь не найден.");
    }

    // Регистрация нового читателя (только администратор)
    public static async Task<int> AddReaderAsync(User reader, string password)
    {
        Session.RequireAdmin();

        reader.LastName = reader.LastName.Trim();
        reader.FirstName = reader.FirstName.Trim();
        reader.Email = reader.Email.Trim();

        if (reader.LastName == "" || reader.FirstName == "")
            throw new AppException("Укажите фамилию и имя.");
        if (!reader.Email.Contains('@') || !reader.Email.Contains('.'))
            throw new AppException("Укажите корректный email.");
        if (password.Length < 4)
            throw new AppException("Пароль должен быть не короче 4 символов.");

        using var db = Db.Create();

        if (await db.Users.AnyAsync(u => u.Email == reader.Email))
            throw new AppException("Пользователь с таким email уже есть.");

        int? roleId = await db.Roles.Where(r => r.Name == "Читатель").Select(r => (int?)r.RoleId).FirstOrDefaultAsync();
        reader.RoleId = roleId ?? throw new AppException("В таблице roles нет роли «Читатель».");
        reader.AdminPassword = password;

        db.Users.Add(reader);
        await db.SaveChangesAsync();
        return reader.UserId;
    }

    public static async Task<List<Role>> GetRolesAsync()
    {
        using var db = Db.Create();
        return await db.Roles.OrderBy(r => r.RoleId).AsNoTracking().ToListAsync();
    }

    // Редактирование данных пользователя и его роли (только администратор)
    public static async Task UpdateUserAsync(User changed)
    {
        Session.RequireAdmin();

        if (string.IsNullOrWhiteSpace(changed.LastName) || string.IsNullOrWhiteSpace(changed.FirstName))
            throw new AppException("Укажите фамилию и имя.");
        if (!changed.Email.Contains('@') || !changed.Email.Contains('.'))
            throw new AppException("Укажите корректный email.");

        // Администратор не может сам себя лишить прав — иначе в системе может не остаться админа
        if (changed.UserId == Session.CurrentUser!.UserId && changed.RoleId != Session.CurrentUser.RoleId)
            throw new AppException("Нельзя изменить роль самому себе.");

        using var db = Db.Create();

        User? user = await db.Users.FindAsync(changed.UserId);
        if (user == null) throw new AppException("Пользователь не найден.");

        string email = changed.Email.Trim();
        if (await db.Users.AnyAsync(u => u.Email == email && u.UserId != user.UserId))
            throw new AppException("Пользователь с таким email уже есть.");
        if (!await db.Roles.AnyAsync(r => r.RoleId == changed.RoleId))
            throw new AppException("Выберите роль.");

        user.LastName = changed.LastName.Trim();
        user.FirstName = changed.FirstName.Trim();
        user.MiddleName = changed.MiddleName;
        user.Email = email;
        user.Phone = changed.Phone;
        user.StudentGroup = changed.StudentGroup;
        user.RoleId = changed.RoleId;

        await db.SaveChangesAsync();
    }
}
