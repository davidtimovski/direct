using Direct.Desktop.ViewModels;
using Microsoft.UI.Xaml;

namespace Direct.Desktop;

public sealed partial class NewContactWindow : Window
{
    public NewContactViewModel ViewModel { get; }

    public NewContactWindow(NewContactViewModel viewModel)
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ViewModel = viewModel;
    }

    private async void AddContact_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.AddContactAsync();
        Close();
    }
}
