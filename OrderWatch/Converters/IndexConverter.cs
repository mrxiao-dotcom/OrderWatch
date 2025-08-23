using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace OrderWatch.Converters;

public class IndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DataGridRow row)
        {
            var dataGrid = FindParent<DataGrid>(row);
            if (dataGrid != null)
            {
                var index = dataGrid.Items.IndexOf(row.DataContext);
                return (index + 1).ToString();
            }
        }
        return "1";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        if (parent == null) return null;
        return parent is T ? (T)parent : FindParent<T>(parent);
    }
}
