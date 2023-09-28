using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Microsoft.UI.Xaml;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Direct.Desktop.ViewModels;

public partial class GeneralSettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    public GeneralSettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        SelectedTheme = _settingsService.Theme.ToString();

        FontSize = _settingsService.MessageFontSize;
        SpellCheckEnabled = _settingsService.SpellCheckEnabled;
    }

    [ObservableProperty]
    private string selectedTheme;

    [ObservableProperty]
    private double fontSize;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpellCheckCheckBoxTooltip))]
    private bool spellCheckEnabled;

    public string SpellCheckCheckBoxTooltip => SpellCheckEnabled ? "Disable" : "Enable";

    partial void OnSelectedThemeChanged(string value)
    {
        _ = Enum.TryParse(value, out ElementTheme theme);

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
