using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Microsoft.UI.Xaml;

namespace Direct.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public SettingsViewModel(
        ISettingsService settingsService,
        GeneralSettingsViewModel generalSettingsViewModel,
        FeaturesSettingsViewModel featuresSettings)
    {
        settingsService.Changed += SettingsChanged;

        Theme = settingsService.Theme;

        GeneralSettings = generalSettingsViewModel;
        FeaturesSettings = featuresSettings;
    }

    [ObservableProperty]
    private ElementTheme theme;

    public GeneralSettingsViewModel GeneralSettings { get; set; }

    public FeaturesSettingsViewModel FeaturesSettings { get; set; }

    private void SettingsChanged(object? _, SettingsChangedEventArgs e)
    {
        if (e.ChangedSetting == Setting.Theme)
        {
            Theme = e.Theme;
        }
    }
}
