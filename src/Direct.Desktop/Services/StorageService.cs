using Microsoft.UI.Xaml;
using Windows.Storage;

namespace Direct.Services;

public interface IStorageService
{
    AppData AppData { get; }
    void Save();
}

public class StorageService : IStorageService
{
    private const int DefaultWindowWidth = 1200;
    private const int DefaultWindowHeight = 800;

    public StorageService()
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
 
        object? windowWidthValue = localSettings.Values[nameof(AppData.WindowWidth)];
        object? windowHeightValue = localSettings.Values[nameof(AppData.WindowHeight)];
        object? themeValue = localSettings.Values[nameof(AppData.Theme)];
        object? passwordHashValue = localSettings.Values[nameof(AppData.PasswordHash)];
        object? nicknameValue = localSettings.Values[nameof(AppData.Nickname)];

        int windowWidth = windowWidthValue is null ? DefaultWindowWidth : (int)windowWidthValue;
        int windowHeight = windowHeightValue is null ? DefaultWindowHeight : (int)windowHeightValue;
        ElementTheme theme = themeValue is null ? ElementTheme.Default : (ElementTheme)themeValue;
        string passwordHash = passwordHashValue is null ? string.Empty : (string)passwordHashValue;
        string nickname = nicknameValue is null ? string.Empty : (string)nicknameValue;

        AppData = new AppData(windowWidth, windowHeight, theme, passwordHash, nickname);
    }

    public AppData AppData { get; }

    public void Save()
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        localSettings.Values[nameof(AppData.WindowWidth)] = AppData.WindowWidth;
        localSettings.Values[nameof(AppData.WindowHeight)] = AppData.WindowHeight;
        localSettings.Values[nameof(AppData.Theme)] = (int)AppData.Theme;
        localSettings.Values[nameof(AppData.PasswordHash)] = AppData.PasswordHash;
        localSettings.Values[nameof(AppData.Nickname)] = AppData.Nickname;
    }
}

public class AppData
{
    public AppData(int windowWidth, int windowHeight, ElementTheme elementTheme, string passwordHash, string nickname)
    {
        WindowWidth = windowWidth;
        WindowHeight = windowHeight;
        Theme = elementTheme;
        PasswordHash = passwordHash;
        Nickname = nickname;
    }

    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }
    public ElementTheme Theme { get; set; }
    public string PasswordHash { get; set; }
    public string Nickname { get; set; } = null!;
}
