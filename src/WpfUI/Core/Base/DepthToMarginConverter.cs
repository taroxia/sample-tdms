// ────────────────────────────────
//
// ────────────────────────────────

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfUI.Core.Base;

public class DepthToMarginConverter : IValueConverter
{
    public double IndentSize { get; set; } = 20.0;
    public object Convert(object v, Type t, object p, CultureInfo c)
        => v is int depth ? new Thickness(depth * IndentSize, 0, 0, 0) : new Thickness(0);
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
