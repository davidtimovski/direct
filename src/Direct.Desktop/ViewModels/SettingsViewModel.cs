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
    }

    [ObservableProperty]
    private ElementTheme theme;

    [ObservableProperty]
    private string selectedTheme;

    public void ThemeChanged()
    {
        _ = Enum.TryParse(SelectedTheme, out ElementTheme theme);

        Theme = theme;
        _settingsService.Theme = theme;
    }
}
