using Direct.Desktop.ViewModels;
using Microsoft.UI.Xaml;

namespace Direct.Desktop;

public sealed partial class EditContactWindow : Window
{
    public EditContactViewModel ViewModel { get; }

    public EditContactWindow(EditContactViewModel viewModel)
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ViewModel = viewModel;
    }

    private async void SaveContact_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveContactAsync();
        Close();
    }
}
