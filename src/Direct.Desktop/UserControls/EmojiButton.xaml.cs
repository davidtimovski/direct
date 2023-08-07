using System.Windows.Input;
using Microsoft.UI.Xaml.Controls;

namespace Direct.Desktop.UserControls;

public sealed partial class EmojiButton : UserControl
{
    public EmojiButton()
    {
        InitializeComponent();
    }

    public string Emoji { get; set; } = null!;
    public ICommand Command { get; set; } = null!;
}
