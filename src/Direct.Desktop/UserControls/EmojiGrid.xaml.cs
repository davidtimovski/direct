using System.Windows.Input;
using Microsoft.UI.Xaml.Controls;

namespace Direct.Desktop.UserControls;

public sealed partial class EmojiGrid : UserControl
{
    public EmojiGrid()
    {
        InitializeComponent();
    }

    public ICommand AddCommand { get; set; } = null!;
}
