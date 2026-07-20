// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfUI.Core.Base;

public sealed class GridLengthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is bool isExpanded && values[1] is double width)
        {
            return isExpanded ? new GridLength(width, GridUnitType.Pixel) : new GridLength(0, GridUnitType.Pixel);
        }
        return new GridLength(260, GridUnitType.Pixel);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        if (value is GridLength gl && gl.GridUnitType == GridUnitType.Pixel)
        {
            // GridSplitter による実リサイズ値の書き戻し（展開中のみ有効化）
            return [Binding.DoNothing, gl.Value];
        }
        return [Binding.DoNothing, Binding.DoNothing];
    }
}
