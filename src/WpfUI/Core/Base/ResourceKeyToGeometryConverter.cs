// ────────────────────────────────
//
// ────────────────────────────────

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfUI.Core.Base;

[ValueConversion(typeof(string), typeof(Geometry))]
public class ResourceKeyToGeometryConverter : IValueConverter
{
    public static ResourceKeyToGeometryConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key && Application.Current.TryFindResource(key) is Geometry geometry)
        {
            return geometry;
        }
        return null; // またはデフォルトのアイコン
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
