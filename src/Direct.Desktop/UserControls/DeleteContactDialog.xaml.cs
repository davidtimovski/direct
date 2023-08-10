using Microsoft.UI.Xaml.Controls;

namespace Direct.Desktop.UserControls;

public sealed partial class DeleteContactDialog : Page
{
    public DeleteContactDialog(string nickname)
    {
        InitializeComponent();

        Text = $"Are you sure you want to delete {nickname}?";
    }

    public string Text { get; }

    public bool DeleteMessages { get; set; }
}
