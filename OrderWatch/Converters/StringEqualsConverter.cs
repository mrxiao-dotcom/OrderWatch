using System;
using System.Globalization;
using System.Windows.Data;

namespace OrderWatch.Converters;

/// <summary>
/// 字符串相等比较转换器，用于单选按钮绑定
/// </summary>
public class StringEqualsConverter : IValueConverter
{
    public static readonly StringEqualsConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue && parameter is string parameterValue)
        {
            return string.Equals(stringValue, parameterValue, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue && parameter is string parameterValue)
        {
            return parameterValue;
        }
        return string.Empty;
    }
}
