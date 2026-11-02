using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Ktpt.CourseOrder.Services; // <-- Заменили Ktpt.CourseOrder
using System;
using System.Collections.Generic;

namespace Ktpt.CourseOrder.Views; // ВАЖНО: Пространство имен должно совпадать с вашим проектом

public static class UiSupport
{
    // Указываем путь к папке Assets внутри программы. 
    // ВНИМАНИЕ: Здесь должно быть имя вашего проекта из .csproj (обычно Ktpt.CourseOrder)
    private const string AssetsRoot = "avares://Ktpt.CourseOrder/Assets/";

    private static readonly Dictionary<string, Bitmap> cache = new();

    // Метод для получения цвета по его имени из App.axaml
    public static IBrush Brush(string key)
    {
        return (IBrush)Application.Current!.Resources[key]!;
    }

    // Есть ли у книги своя обложка в Assets/Images
    public static bool HasPicture(string? fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName) &&
               AssetLoader.Exists(new Uri(AssetsRoot + "Images/" + fileName));
    }

    // Метод для загрузки картинки. Если файла нет, подставляет заглушку picture.png
    public static Bitmap Picture(string? fileName)
    {
        string uri = AssetsRoot + "Images/" + fileName;

        // Если имя пустое или файла не существует — берем заглушку
        if (string.IsNullOrWhiteSpace(fileName) || !AssetLoader.Exists(new Uri(uri)))
        {
            uri = AssetsRoot + "picture.png";
        }

        if (!cache.TryGetValue(uri, out Bitmap? bitmap))
        {
            using var stream = AssetLoader.Open(new Uri(uri));
            bitmap = new Bitmap(stream);
            cache[uri] = bitmap;
        }
        return bitmap;
    }

    // Метод для превращения технической ошибки в понятный текст
    public static string ErrorText(Exception error)
    {
        // Если это наша понятная ошибка из AppException — показываем её текст
        if (error is AppException) return error.Message;

        // Для остальных ошибок показываем общее сообщение
        return "Операция не выполнена: " + error.Message;
    }
}