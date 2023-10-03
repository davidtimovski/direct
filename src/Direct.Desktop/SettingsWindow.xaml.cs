using Direct.Desktop.Pages;
using Direct.Desktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace Direct.Desktop;

public sealed partial class SettingsWindow : Window
{
    public SettingsViewModel ViewModel { get; }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        AppWindow.Resize(new SizeInt32(400, 300));
        InitializeComponent();
        Title = "Settings";

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
