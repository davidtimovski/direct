using Direct.Desktop.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Direct.Desktop.Pages;

public sealed partial class SettingsFeaturesPage : Page
{
    public FeaturesSettingsViewModel ViewModel { get; }

    public SettingsFeaturesPage(FeaturesSettingsViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
    }
}
