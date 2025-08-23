using OrderWatch.ViewModels;
using System.Windows;

namespace OrderWatch.Views;

public partial class TradeHistoryWindow : Window
{
    public TradeHistoryWindow()
    {
        InitializeComponent();
        DataContext = new TradeHistoryViewModel();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
