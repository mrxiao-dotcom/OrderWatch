using OrderWatch.ViewModels;
using System.Windows;

namespace OrderWatch.Views;

public partial class TradeHistoryWindow : Window
{
    public TradeHistoryWindow()
    {
        InitializeComponent();
        DataContext = new TradeHistoryViewModel();
        
        // 窗口加载后再初始化数据
        Loaded += TradeHistoryWindow_Loaded;
    }

    private async void TradeHistoryWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TradeHistoryViewModel viewModel)
        {
            try
            {
                await viewModel.QueryCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                viewModel.StatusMessage = $"加载数据失败: {ex.Message}";
                Console.WriteLine($"TradeHistoryWindow 加载数据失败: {ex.Message}");
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
