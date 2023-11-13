using Direct.Desktop.ViewModels;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Direct.Desktop;

public sealed partial class ProfileWindow : Window
{
    public ProfileViewModel ViewModel { get; }

    public ProfileWindow(ProfileViewModel viewModel)
    {
        AppWindow.Resize(new SizeInt32(450, 550));
        InitializeComponent();
        Title = "Profile";

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ViewModel = viewModel;
    }
}
