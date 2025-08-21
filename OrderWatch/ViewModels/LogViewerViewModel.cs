using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderWatch.Models;
using OrderWatch.Services;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using System.IO;

namespace OrderWatch.ViewModels;

public partial class LogViewerViewModel : ObservableObject
{
    private readonly ILogService _logService;
    
    public LogViewerViewModel()
    {
        _logService = new LogService();
        
        // 初始化集合
        Logs = new ObservableCollection<LogEntry>();
        Categories = new ObservableCollection<string> { "全部", "General", "Trading", "API", "System" };
        
        // 设置默认值
        StartDate = DateTime.Today.AddDays(-7);
        EndDate = DateTime.Today;
        SelectedCategory = "全部";
        
        // 加载初始数据
        _ = LoadLogsAsync();
    }
    
    #region 属性
    
    [ObservableProperty]
    private DateTime _startDate;
    
    [ObservableProperty]
    private DateTime _endDate;
    
    [ObservableProperty]
    private string? _selectedCategory;
    
    [ObservableProperty]
    private bool _isLoading;
    
    #endregion
    
    #region 集合
    
    public ObservableCollection<LogEntry> Logs { get; }
    public ObservableCollection<string> Categories { get; }
    
    #endregion
    
    #region 命令
    
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadLogsAsync();
    }
    
    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV文件 (*.csv)|*.csv|文本文件 (*.txt)|*.txt",
                FileName = $"日志导出_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };
            
            if (saveFileDialog.ShowDialog() == true)
            {
                await ExportLogsToFileAsync(saveFileDialog.FileName);
                MessageBox.Show("日志导出成功！", "导出完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败: {ex.Message}", "导出错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    [RelayCommand]
    private async Task CleanupAsync()
    {
        try
        {
            var result = MessageBox.Show("确定要清理30天前的日志吗？", "确认清理", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                await _logService.CleanupExpiredLogsAsync(30);
                await LoadLogsAsync();
                MessageBox.Show("日志清理完成！", "清理完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"清理失败: {ex.Message}", "清理错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    #endregion
    
    #region 私有方法
    
    private async Task LoadLogsAsync()
    {
        try
        {
            IsLoading = true;
            
            var category = SelectedCategory == "全部" ? null : SelectedCategory;
            var logs = await _logService.GetLogsAsync(StartDate, EndDate, category, 1000);
            
            Logs.Clear();
            foreach (var log in logs)
            {
                Logs.Add(log);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载日志失败: {ex.Message}", "加载错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task ExportLogsToFileAsync(string filePath)
    {
        var lines = new List<string>();
        
        // 添加标题行
        lines.Add("时间,级别,类别,消息,合约,操作,价格,数量,结果");
        
        // 添加数据行
        foreach (var log in Logs)
        {
            var line = $"{log.FormattedTimestamp}," +
                      $"{log.Level}," +
                      $"{log.Category}," +
                      $"\"{log.Message.Replace("\"", "\"\"")}\"," +
                      $"{log.Symbol ?? ""}," +
                      $"{log.Action ?? ""}," +
                      $"{log.Price?.ToString("F4") ?? ""}," +
                      $"{log.Quantity?.ToString("F4") ?? ""}," +
                      $"{log.Result ?? ""}";
            lines.Add(line);
        }
        
        await File.WriteAllLinesAsync(filePath, lines);
    }
    
    #endregion
    
    #region 属性变化处理
    
    partial void OnStartDateChanged(DateTime value)
    {
        _ = LoadLogsAsync();
    }
    
    partial void OnEndDateChanged(DateTime value)
    {
        _ = LoadLogsAsync();
    }
    
    partial void OnSelectedCategoryChanged(string? value)
    {
        _ = LoadLogsAsync();
    }
    
    #endregion
}
