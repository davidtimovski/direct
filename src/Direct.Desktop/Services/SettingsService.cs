using System;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace Direct.Desktop.Services;

public interface ISettingsService
{
    int WindowWidth { get; set; }
    int WindowHeight { get; set; }
    Guid? UserId { get; set; }
    ElementTheme Theme { get; set; }
    double MessageFontSize { get; set; }
    bool SpellCheckEnabled { get; set; }
    bool EmojiPickerEnabled { get; set; }

    event EventHandler<SettingsChangedEventArgs>? Changed;

    void Save();
}

public class SettingsService : ISettingsService
{
    private const int DefaultWindowWidth = 1200;
    private const int DefaultWindowHeight = 800;
    private const double DefaultMessageFontSize = 14;

    public SettingsService()
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        object? windowWidthValue = localSettings.Values[nameof(WindowWidth)];
        object? windowHeightValue = localSettings.Values[nameof(WindowHeight)];
        object? userIdValue = localSettings.Values[nameof(UserId)];
        object? themeValue = localSettings.Values[nameof(Theme)];
        object? messageFontSizeValue = localSettings.Values[nameof(MessageFontSize)];
        object? spellCheckEnabledValue = localSettings.Values[nameof(SpellCheckEnabled)];
        object? emojiPickerEnabledValue = localSettings.Values[nameof(EmojiPickerEnabled)];

        WindowWidth = windowWidthValue is null ? DefaultWindowWidth : (int)windowWidthValue;
        WindowHeight = windowHeightValue is null ? DefaultWindowHeight : (int)windowHeightValue;
        UserId = userIdValue is null ? null : new Guid((string)userIdValue);
        _theme = themeValue is null ? ElementTheme.Default : (ElementTheme)themeValue;
        _messageFontSize = messageFontSizeValue is null ? DefaultMessageFontSize : (double)messageFontSizeValue;
        _spellCheckEnabled = spellCheckEnabledValue is null || (bool)spellCheckEnabledValue;
        _emojiPickerEnabled = emojiPickerEnabledValue is not null && (bool)emojiPickerEnabledValue;
    }

    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }

    public Guid? UserId { get; set; }

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
                Changed?.Invoke(this, CurrentSettingsEventArgs());
            }
        }
    }

    private double _messageFontSize;
    public double MessageFontSize
    {
        get => _messageFontSize;
        set
        {
            if (_messageFontSize != value)
            {
                _messageFontSize = value;
                Save();
                Changed?.Invoke(this, CurrentSettingsEventArgs());
            }
        }
    }

    private bool _spellCheckEnabled;
    public bool SpellCheckEnabled
    {
        get => _spellCheckEnabled;
        set
        {
            if (_spellCheckEnabled != value)
            {
                _spellCheckEnabled = value;
                Save();
                Changed?.Invoke(this, CurrentSettingsEventArgs());
            }
        }
    }

    private bool _emojiPickerEnabled;
    public bool EmojiPickerEnabled
    {
        get => _emojiPickerEnabled;
        set
        {
            if (_emojiPickerEnabled != value)
            {
                _emojiPickerEnabled = value;
                Save();
                Changed?.Invoke(this, CurrentSettingsEventArgs());
            }
        }
    }

    public event EventHandler<SettingsChangedEventArgs>? Changed;

    public void Save()
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        localSettings.Values[nameof(WindowWidth)] = WindowWidth;
        localSettings.Values[nameof(WindowHeight)] = WindowHeight;
        localSettings.Values[nameof(UserId)] = UserId?.ToString();
        localSettings.Values[nameof(Theme)] = (int)Theme;
        localSettings.Values[nameof(MessageFontSize)] = MessageFontSize;
        localSettings.Values[nameof(SpellCheckEnabled)] = SpellCheckEnabled;
        localSettings.Values[nameof(EmojiPickerEnabled)] = EmojiPickerEnabled;
    }

    private SettingsChangedEventArgs CurrentSettingsEventArgs()
    {
        return new SettingsChangedEventArgs
        {
            Theme = _theme,
            MessageFontSize = _messageFontSize,
            SpellCheckEnabled = _spellCheckEnabled,
            EmojiPickerEnabled = _emojiPickerEnabled
        };
    }
}

public class SettingsChangedEventArgs : EventArgs
{
    public required ElementTheme Theme { get; init; }
    public required double MessageFontSize { get; init; }
    public required bool SpellCheckEnabled { get; init; }
    public required bool EmojiPickerEnabled { get; init; }
}
