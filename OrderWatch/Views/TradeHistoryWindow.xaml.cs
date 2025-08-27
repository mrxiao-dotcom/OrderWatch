using OrderWatch.ViewModels;
using System;
using System.Windows;

namespace OrderWatch.Views;

public partial class TradeHistoryWindow : Window
{
    public TradeHistoryWindow()
    {
        try
        {
            InitializeComponent();
            
            // 先设置一个空的DataContext避免立即崩溃
            DataContext = null;
            
            // 延迟初始化ViewModel
            Loaded += TradeHistoryWindow_Loaded;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"初始化交易历史窗口失败: {ex.Message}\n\n详细信息: {ex.StackTrace}", "初始化错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void TradeHistoryWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // 在窗口加载后创建ViewModel
            var viewModel = new TradeHistoryViewModel();
            DataContext = viewModel;
            
                         // 设置初始状态，不自动加载数据
             viewModel.StatusMessage = "就绪 - 点击查询按钮加载交易历史";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"创建交易历史视图模型失败: {ex.Message}\n\n详细信息: {ex.StackTrace}", "视图模型创建错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"关闭窗口失败: {ex.Message}", "关闭错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
} 