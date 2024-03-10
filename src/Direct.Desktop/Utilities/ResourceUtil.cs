using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Direct.Desktop.Utilities;

internal static class ResourceUtil
{
    internal static SolidColorBrush GetBrush(string resourceName)
        => (Application.Current.Resources[resourceName] as SolidColorBrush)!;
}
