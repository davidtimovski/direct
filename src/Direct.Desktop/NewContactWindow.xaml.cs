using Direct.ViewModels;
using Microsoft.UI.Xaml;

namespace Direct;

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
}
