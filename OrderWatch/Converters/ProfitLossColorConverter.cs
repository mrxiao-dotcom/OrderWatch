using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OrderWatch.Converters;

/// <summary>
/// 持仓浮盈颜色转换器
/// 盈利（> 0）显示红色，亏损（< 0）显示绿色，持平显示默认颜色
/// </summary>
public class ProfitLossColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal profit)
        {
            if (profit > 0)
            {
                return Brushes.Red; // 盈利用红色
            }
            else if (profit < 0)
            {
                return Brushes.Green; // 亏损用绿色
            }
            else
            {
                return Brushes.Black; // 持平用黑色
            }
        }

        return Brushes.Black; // 默认黑色
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 