// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace WpfUI.Core.Base;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool IsInverse { get; set; }
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        (bool)v ^ IsInverse ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
