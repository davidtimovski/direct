using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Microsoft.UI.Xaml;

namespace Direct.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        Theme = _settingsService.Theme;
        SelectedTheme = Theme.ToString();

        FontSize = _settingsService.MessageFontSize;
        SpellCheckEnabled = _settingsService.SpellCheckEnabled;
    }

    [ObservableProperty]
    private ElementTheme theme;

    [ObservableProperty]
    private double fontSize;

    [ObservableProperty]
    private bool spellCheckEnabled;

    [ObservableProperty]
    private string selectedTheme;

    partial void OnSelectedThemeChanged(string value)
    {
        _ = Enum.TryParse(value, out ElementTheme theme);

        Theme = theme;
        _settingsService.Theme = theme;
    }

    partial void OnFontSizeChanged(double value)
    {
        _settingsService.MessageFontSize = value;
    }

    partial void OnSpellCheckEnabledChanged(bool value)
    {
        _settingsService.SpellCheckEnabled = value;
    }
}
