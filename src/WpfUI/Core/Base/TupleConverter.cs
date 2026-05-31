namespace WpfUI.Core.Base;

using System;
using System.Globalization;
using System.Windows.Data;

public class TupleConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2)
        {
            return (values[0], values[1]);
        }
        return Tuple.Create(values);
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
