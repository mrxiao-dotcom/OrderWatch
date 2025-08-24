using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OrderWatch.Converters;

public class PriceChangeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string changePercentStr && !string.IsNullOrEmpty(changePercentStr))
        {
            if (changePercentStr.StartsWith("+"))
                return Brushes.Red;   // 涨用红色
            else if (changePercentStr.StartsWith("-"))
                return Brushes.Green; // 跌用绿色
            else
                return Brushes.Black; // 平用黑色
        }
        return Brushes.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 