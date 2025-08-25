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
            InitializeComponent();
            
            // 注册窗口关闭事件
            this.Closed += MainWindow_Closed;
            
            // 延迟创建服务，先显示窗口
            this.Loaded += MainWindow_Loaded;
        }
        catch (Exception ex)
        {
            // 显示错误信息
            MessageBox.Show($"主窗口初始化异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            
            // 创建TestViewModel（包含完善的限价单、做多/做空条件单功能）
            Console.WriteLine("创建 TestViewModel...");
            var testViewModel = new TestViewModel();
            
            Console.WriteLine("设置 DataContext...");
            this.DataContext = testViewModel;
            
            // 设置窗口标题为版本信息
            this.Title = testViewModel.WindowTitle;
            
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
        var dataGrid = sender as DataGrid;
        if (dataGrid?.SelectedItem is CandidateSymbol selectedSymbol)
        {
            if (DataContext is MainViewModel mainViewModel)
            {
                // 自动填写合约名称到所有下单区，并获取合约信息
                await mainViewModel.AutoFillSymbolToOrderAreasAsync(selectedSymbol.Symbol);
            }
            else if (DataContext is ViewModels.TestViewModel testViewModel)
            {
                // TestViewModel的自动填充功能
                await testViewModel.AutoFillSymbolToOrderAreasAsync(selectedSymbol.Symbol);
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
        if (sender is TextBlock textBlock)
        {
            if (DataContext is MainViewModel viewModel)
            {
                var selectedSymbol = textBlock.Text;
                viewModel.NewSymbolInput = selectedSymbol;
                viewModel.SymbolSuggestions.Clear();
                
                // 自动添加选中的合约
                await viewModel.AddSymbolFromInputAsync();
            }
            else if (DataContext is ViewModels.TestViewModel testViewModel && textBlock.DataContext is string symbol)
            {
                var index = testViewModel.SymbolSuggestions.IndexOf(symbol);
                if (index >= 0)
                {
                    SelectSuggestion(index);
                    e.Handled = true;
                }
            }
        }
    }

    /// <summary>
    /// 双击持仓列表，自动填写到下单区
    /// </summary>
    private async void PositionsDataGrid_MouseDoubleClick(object? sender, MouseButtonEventArgs e)
    {
        var dataGrid = sender as DataGrid;
        if (dataGrid?.SelectedItem is PositionInfo selectedPosition)
        {
            if (DataContext is MainViewModel mainViewModel)
            {
                // 自动填写持仓信息到下单区
                await mainViewModel.AutoFillPositionToOrderAreasAsync(selectedPosition);
            }
            else if (DataContext is ViewModels.TestViewModel testViewModel)
            {
                // TestViewModel的自动填充功能
                await testViewModel.AutoFillPositionToOrderAreasAsync(selectedPosition);
            }
        }
    }

    #region 合约输入自动完成事件处理

    /// <summary>
    /// 合约输入框文本变化事件
    /// </summary>
    private async void SymbolInputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is ViewModels.TestViewModel testViewModel && sender is TextBox textBox)
        {
            var input = textBox.Text?.Trim();
            if (!string.IsNullOrEmpty(input))
            {
                // 获取合约建议
                var suggestions = await testViewModel.GetSymbolSuggestionsAsync(input);
                testViewModel.SymbolSuggestions.Clear();
                foreach (var suggestion in suggestions)
                {
                    testViewModel.SymbolSuggestions.Add(suggestion);
                }
                
                // 显示建议列表
                SymbolSuggestionsPopup.IsOpen = testViewModel.SymbolSuggestions.Count > 0;
            }
            else
            {
                SymbolSuggestionsPopup.IsOpen = false;
                testViewModel.SymbolSuggestions.Clear();
            }
        }
    }

    /// <summary>
    /// 合约输入框按键事件
    /// </summary>
    private void SymbolInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
        {
            if (SymbolSuggestionsPopup.IsOpen && SymbolSuggestionsList.Items.Count > 0)
            {
                e.Handled = true;
                SymbolSuggestionsList.SelectedIndex = 0;
                SymbolSuggestionsList.Focus();
                SymbolSuggestionsList.ScrollIntoView(SymbolSuggestionsList.SelectedItem);
            }
        }
        else if (e.Key == Key.Up)
        {
            if (SymbolSuggestionsPopup.IsOpen && SymbolSuggestionsList.Items.Count > 0)
            {
                e.Handled = true;
                SymbolSuggestionsList.SelectedIndex = SymbolSuggestionsList.Items.Count - 1;
                SymbolSuggestionsList.Focus();
                SymbolSuggestionsList.ScrollIntoView(SymbolSuggestionsList.SelectedItem);
            }
        }
        else if (e.Key == Key.Enter)
        {
            if (SymbolSuggestionsPopup.IsOpen && SymbolSuggestionsList.Items.Count > 0)
            {
                // 选择第一个建议
                SelectSuggestion(0);
                e.Handled = true;
            }
            else if (DataContext is ViewModels.TestViewModel testViewModel)
            {
                // 直接添加输入的合约
                testViewModel.AddSymbolFromInputCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            SymbolSuggestionsPopup.IsOpen = false;
            e.Handled = true;
        }
    }

    /// <summary>
    /// 合约输入框失去焦点事件
    /// </summary>
    private async void SymbolInputBox_LostFocus(object sender, RoutedEventArgs e)
    {
        // 基本的失去焦点处理
        await Task.Delay(100);
    }

    /// <summary>
    /// 建议列表按键事件
    /// </summary>
    private void SymbolSuggestionsList_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is ListBox listBox)
        {
            if (e.Key == Key.Enter)
            {
                // 选择当前项
                if (listBox.SelectedIndex >= 0)
                {
                    SelectSuggestion(listBox.SelectedIndex);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape)
            {
                SymbolSuggestionsPopup.IsOpen = false;
                SymbolInputBox.Focus();
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// 建议列表双击事件
    /// </summary>
    private void SymbolSuggestionsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedIndex >= 0)
        {
            SelectSuggestion(listBox.SelectedIndex);
            e.Handled = true;
        }
    }

    /// <summary>
    /// 建议列表选择变化事件
    /// </summary>
    private void SymbolSuggestionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 可以在这里添加选择变化的处理逻辑
    }

    /// <summary>
    /// 选择建议项
    /// </summary>
    private void SelectSuggestion(int index)
    {
        if (DataContext is ViewModels.TestViewModel testViewModel && 
            index >= 0 && index < testViewModel.SymbolSuggestions.Count)
        {
            var selectedSymbol = testViewModel.SymbolSuggestions[index];
            testViewModel.NewSymbolInput = selectedSymbol;
            SymbolSuggestionsPopup.IsOpen = false;
            
            // 执行添加命令
            testViewModel.AddSymbolFromInputCommand.Execute(null);
        }
    }



    /// <summary>
    /// 执行添加合约命令
    /// </summary>
    private void ExecuteAddSymbolCommand()
    {
        if (DataContext is ViewModels.TestViewModel testViewModel)
        {
            testViewModel.AddSymbolFromInputCommand.Execute(null);
        }
    }

    #endregion
}