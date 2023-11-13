using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Microsoft.UI.Xaml;

namespace Direct.Desktop.ViewModels;

public partial class GeneralSettingsViewModel : ObservableObject
{
    private const string DefaultThemeName = "System default";

    private readonly ISettingsService _settingsService;

    public GeneralSettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        SelectedTheme = _settingsService.Theme == ElementTheme.Default
            ? DefaultThemeName
            : _settingsService.Theme.ToString();

        FontSize = _settingsService.MessageFontSize;
        SpellCheckEnabled = _settingsService.SpellCheckEnabled;
    }

    [ObservableProperty]
    private string selectedTheme;

    [ObservableProperty]
    private double fontSize;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpellCheckToggleTooltip))]
    private bool spellCheckEnabled;

    public string SpellCheckToggleTooltip => SpellCheckEnabled ? "Disable" : "Enable";

    partial void OnSelectedThemeChanged(string value)
    {
        _settingsService.Theme = value == DefaultThemeName
            ? ElementTheme.Default
            : Enum.Parse<ElementTheme>(value);
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
