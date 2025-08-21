using OrderWatch.Models;
using System.Windows;

namespace OrderWatch.Views;

public partial class ConditionalOrderWindow : Window
{
    public ConditionalOrder ConditionalOrder { get; private set; }

    public ConditionalOrderWindow(ConditionalOrder? conditionalOrder = null)
    {
        InitializeComponent();
        
        if (conditionalOrder != null)
        {
            ConditionalOrder = conditionalOrder;
            DataContext = ConditionalOrder;
        }
        else
        {
            ConditionalOrder = new ConditionalOrder
            {
                Side = "SELL",
                Type = "STOP_LOSS",
                Quantity = 0,
                TriggerPrice = 0,
                OrderPrice = 0,
                Remark = string.Empty
            };
            DataContext = ConditionalOrder;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // 验证必填字段
        if (string.IsNullOrWhiteSpace(ConditionalOrder.Symbol))
        {
            MessageBox.Show("请输入合约名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ConditionalOrder.Quantity <= 0)
        {
            MessageBox.Show("请输入有效的数量", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ConditionalOrder.TriggerPrice <= 0)
        {
            MessageBox.Show("请输入有效的触发价格", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 如果是限价单，需要验证订单价格
        if (ConditionalOrder.Type == "STOP_LIMIT" && ConditionalOrder.OrderPrice <= 0)
        {
            MessageBox.Show("限价单需要输入有效的订单价格", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
