using Microsoft.Win32;
using OrderWatch.ViewModels;
using System.Windows;

namespace OrderWatch.Views;

public partial class LogViewerWindow : Window
{
    public LogViewerWindow()
    {
        InitializeComponent();
        DataContext = new LogViewerViewModel();
        
        // 设置默认日期范围（最近7天）
        var viewModel = DataContext as LogViewerViewModel;
        if (viewModel != null)
        {
            viewModel.StartDate = DateTime.Today.AddDays(-7);
            viewModel.EndDate = DateTime.Today;
        }
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
