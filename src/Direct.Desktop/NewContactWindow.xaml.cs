using Direct.Desktop.ViewModels;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Direct.Desktop;

public sealed partial class NewContactWindow : Window
{
    public NewContactViewModel ViewModel { get; }

    public NewContactWindow(NewContactViewModel viewModel)
    {
        AppWindow.Resize(new SizeInt32(370, 280));
        InitializeComponent();
        Title = "Add new contact";

        UserIdTextBox.Loaded += UserIdTextBox_Loaded;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ViewModel = viewModel;
    }

    private void UserIdTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        UserIdTextBox.Focus(FocusState.Programmatic);
    }

    private async void AddContact_Click(object _, RoutedEventArgs e)
    {
        await ViewModel.AddContactAsync();
        Close();
    }
}
