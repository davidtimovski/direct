using Direct.Desktop.Pages;
using Direct.Desktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Direct.Desktop;

public sealed partial class SettingsWindow : Window
{
    public SettingsViewModel ViewModel { get; }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ViewModel = viewModel;
    }

    private void NavigationView_SelectionChanged(NavigationView _, NavigationViewSelectionChangedEventArgs args)
    {
        // Should be done with ContentFrame.NavigateToType() but impossible to pass in (inject) Page constructor parameters

        if ((NavigationViewItem)args.SelectedItem == GeneralItem)
        {
            ContentFrame.Content = new SettingsGeneralPage(ViewModel.GeneralSettings);
        }
        else if ((NavigationViewItem)args.SelectedItem == FeaturesItem)
        {
            ContentFrame.Content = new SettingsFeaturesPage(ViewModel.FeaturesSettings);
        }
    }
}
