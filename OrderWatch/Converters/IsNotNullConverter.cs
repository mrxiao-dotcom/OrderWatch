using System;
using System.Globalization;
using System.Windows.Data;

namespace OrderWatch.Converters;

public class IsNotNullConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// 用于静态访问的静态类
public static class Converters
{
    public static readonly IsNotNullConverter IsNotNullConverter = new();
} 