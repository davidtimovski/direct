using Direct.Desktop.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Direct.Desktop.Pages;

public sealed partial class SettingsGeneralPage : Page
{
    public GeneralSettingsViewModel ViewModel { get; }

    public SettingsGeneralPage(GeneralSettingsViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
    }
}
