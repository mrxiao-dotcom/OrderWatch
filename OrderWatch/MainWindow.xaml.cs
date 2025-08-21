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

namespace OrderWatch;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 双击候选币列表，自动填写到下单区
    /// </summary>
    private async void CandidateSymbolsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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
    private async void CandidateSymbolsDataGrid_KeyDown(object sender, KeyEventArgs e)
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
        }
    }
}