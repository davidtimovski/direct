using Direct.Desktop.ViewModels;
using Microsoft.UI.Xaml;

namespace Direct.Desktop;

public sealed partial class NewContactWindow : Window
{
    public NewContactViewModel ViewModel { get; }

    public NewContactWindow(NewContactViewModel viewModel)
    {
        InitializeComponent();
        Title = "Add new contact";

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ViewModel = viewModel;
    }

    private async void AddContact_Click(object _, RoutedEventArgs e)
    {
        await ViewModel.AddContactAsync();
        Close();
    }
}
