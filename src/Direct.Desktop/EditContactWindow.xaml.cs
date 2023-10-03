using Direct.Desktop.ViewModels;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Direct.Desktop;

public sealed partial class EditContactWindow : Window
{
    public EditContactViewModel ViewModel { get; }

    public EditContactWindow(EditContactViewModel viewModel)
    {
        AppWindow.Resize(new SizeInt32(370, 280));
        InitializeComponent();
        Title = "Edit contact";

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ViewModel = viewModel;
    }

    private async void SaveContact_Click(object _, RoutedEventArgs e)
    {
        await ViewModel.SaveContactAsync();
        Close();
    }
}
