using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Direct.Utilities;

internal static class ResourceUtil
{
    internal static SolidColorBrush GetBrush(string resourceName)
    {
        return (Application.Current.Resources[resourceName] as SolidColorBrush)!;
    }
}
