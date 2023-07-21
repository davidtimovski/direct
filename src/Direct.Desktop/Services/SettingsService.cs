using System;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace Direct.Desktop.Services;

public interface ISettingsService
{
    int WindowWidth { get; set; }
    int WindowHeight { get; set; }
    ElementTheme Theme { get; set; }
    Guid UserId { get; set; }
    string Nickname { get; set; }

    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    void Save();
}

public class SettingsService : ISettingsService
{
    private const int DefaultWindowWidth = 1200;
    private const int DefaultWindowHeight = 800;

    public SettingsService()
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        object? windowWidthValue = localSettings.Values[nameof(WindowWidth)];
        object? windowHeightValue = localSettings.Values[nameof(WindowHeight)];
        object? themeValue = localSettings.Values[nameof(Theme)];
        object? userIdValue = localSettings.Values[nameof(UserId)];
        object? nicknameValue = localSettings.Values[nameof(Nickname)];

        WindowWidth = windowWidthValue is null ? DefaultWindowWidth : (int)windowWidthValue;
        WindowHeight = windowHeightValue is null ? DefaultWindowHeight : (int)windowHeightValue;
        _theme = themeValue is null ? ElementTheme.Default : (ElementTheme)themeValue;
        UserId = userIdValue is null ? Guid.NewGuid() : new Guid((string)userIdValue);
        Nickname = nicknameValue is null ? string.Empty : (string)nicknameValue;
    }

    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }

    private ElementTheme _theme;
    public ElementTheme Theme
    {
        get => _theme; 
        set
        {
            if (_theme != value)
            {
                _theme = value;
                Save();
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(value));
            }
        }
    }

    public Guid UserId { get; set; }
    public string Nickname { get; set; } = null!;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public void Save()
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        localSettings.Values[nameof(WindowWidth)] = WindowWidth;
        localSettings.Values[nameof(WindowHeight)] = WindowHeight;
        localSettings.Values[nameof(Theme)] = (int)Theme;
        localSettings.Values[nameof(UserId)] = UserId.ToString();
        localSettings.Values[nameof(Nickname)] = Nickname;
    }
}

public class ThemeChangedEventArgs : EventArgs
{
    public ThemeChangedEventArgs(ElementTheme theme)
    {
        Theme = theme;
    }

    public ElementTheme Theme { get; init; }
}
