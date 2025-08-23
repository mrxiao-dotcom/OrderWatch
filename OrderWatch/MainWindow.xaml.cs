using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OrderWatch.ViewModels;
using OrderWatch.Models;
using OrderWatch.Services;
using System;

namespace OrderWatch;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        try
        {
            Console.WriteLine("=== MainWindow构造函数开始 ===");
            InitializeComponent();
            Console.WriteLine("InitializeComponent完成");
            
            // 注册窗口关闭事件
            this.Closed += MainWindow_Closed;
            Console.WriteLine("事件注册完成");
            
            // 延迟创建服务，先显示窗口
            this.Loaded += MainWindow_Loaded;
            Console.WriteLine("MainWindow构造函数完成 - 延迟加载模式");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MainWindow构造函数异常: {ex.Message}");
            Console.WriteLine($"异常堆栈: {ex.StackTrace}");
            
            // 显示错误信息
            MessageBox.Show($"主窗口初始化异常: {ex.Message}\n\n详细信息请查看控制台", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 窗口加载完成后初始化服务和ViewModel
    /// </summary>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Console.WriteLine("=== 开始加载服务和ViewModel ===");
            
            // 创建服务实例
            Console.WriteLine("创建 BinanceService...");
            var binanceService = new BinanceService();
            
            Console.WriteLine("创建 ConfigService...");
            var configService = new ConfigService();
            
            Console.WriteLine("创建 ConditionalOrderService...");
            var conditionalOrderService = new ConditionalOrderService(binanceService);
            
            Console.WriteLine("创建 SymbolInfoService...");
            var symbolInfoService = new SymbolInfoService(binanceService);
            
            Console.WriteLine("创建 BinanceSymbolService...");
            var binanceSymbolService = new BinanceSymbolService();
            
            Console.WriteLine("所有服务创建完成");
            
            // 创建ViewModel
            Console.WriteLine("创建 MainViewModel...");
            var viewModel = new MainViewModel(binanceService, configService, conditionalOrderService, symbolInfoService, binanceSymbolService);
            
            Console.WriteLine("设置 DataContext...");
            this.DataContext = viewModel;
            
            Console.WriteLine("=== 服务和ViewModel加载完成 ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"服务加载异常: {ex.Message}");
            Console.WriteLine($"异常堆栈: {ex.StackTrace}");
            
            MessageBox.Show($"服务加载失败: {ex.Message}\n\n程序将以简化模式运行", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// 窗口关闭事件处理
    /// </summary>
    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        try
        {
            Console.WriteLine("=== 主窗口正在关闭 ===");
            
            if (DataContext is MainViewModel viewModel)
            {
                // 强制退出，清理所有资源
                viewModel.ForceExit();
            }
            
            // 确保应用程序退出
            Application.Current?.Shutdown();
            Console.WriteLine("=== 主窗口关闭完成 ===");
        }
        catch (Exception ex)
        {
            // 静默处理异常，避免影响程序退出
            Console.WriteLine($"窗口关闭异常: {ex.Message}");
        }
        finally
        {
            // 最后手段：强制退出
            Environment.Exit(0);
        }
    }

    /// <summary>
    /// 双击候选币列表，自动填写到下单区
    /// </summary>
    private async void CandidateSymbolsDataGrid_MouseDoubleClick(object? sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid?.SelectedItem is CandidateSymbol selectedSymbol)
            {
                // 自动填写合约名称到所有下单区，并获取合约信息
                await viewModel.AutoFillSymbolToOrderAreasAsync(selectedSymbol.Symbol);
            }
        }
    }

    /// <summary>
    /// 候选币列表键盘事件处理
    /// </summary>
    private async void CandidateSymbolsDataGrid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            // Ctrl+V 粘贴合约
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                await viewModel.PasteSymbolAsync();
            }
            // Delete 删除选中的合约
            else if (e.Key == Key.Delete)
            {
                e.Handled = true;
                await viewModel.RemoveCandidateSymbolAsync();
            }
            // Enter 添加输入框中的合约
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await viewModel.AddSymbolFromInputAsync();
            }
        }
    }

    /// <summary>
    /// 合约建议点击事件
    /// </summary>
    private async void SymbolSuggestion_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && sender is TextBlock textBlock)
        {
            var selectedSymbol = textBlock.Text;
            viewModel.NewSymbolInput = selectedSymbol;
            viewModel.SymbolSuggestions.Clear();
            
            // 自动添加选中的合约
            await viewModel.AddSymbolFromInputAsync();
        }
    }

    /// <summary>
    /// 双击持仓列表，自动填写到下单区
    /// </summary>
    private async void PositionsDataGrid_MouseDoubleClick(object? sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid?.SelectedItem is PositionInfo selectedPosition)
            {
                // 自动填写持仓信息到下单区
                await viewModel.AutoFillPositionToOrderAreasAsync(selectedPosition);
            }
        }
    }
}