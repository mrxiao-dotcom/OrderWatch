using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderWatch.Models;
using OrderWatch.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;

namespace OrderWatch.ViewModels;

public partial class TradeHistoryViewModel : ObservableObject
{
    private readonly ITradeHistoryService _tradeHistoryService;

    public TradeHistoryViewModel()
    {
        _tradeHistoryService = new TradeHistoryService();
        
        // 初始化日期范围（最近7天）
        StartDate = DateTime.Today.AddDays(-7);
        EndDate = DateTime.Today;
        
        TradeHistories = new ObservableCollection<TradeHistory>();
        
        // 设置初始状态，延迟加载数据
        StatusMessage = "正在初始化...";
    }

    #region 属性

    [ObservableProperty]
    private DateTime _startDate;

    [ObservableProperty]
    private DateTime _endDate;

    [ObservableProperty]
    private string _filterSymbol = string.Empty;

    [ObservableProperty]
    private TradeHistory? _selectedTradeHistory;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private bool _isLoading;

    #endregion

    #region 集合

    public ObservableCollection<TradeHistory> TradeHistories { get; }

    #endregion

    #region 命令

    [RelayCommand]
    private async Task QueryAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在查询交易历史...";

            await LoadTradeHistoriesAsync();

            StatusMessage = $"查询完成，共找到 {TradeHistories.Count} 条记录";
        }
        catch (Exception ex)
        {
            StatusMessage = $"查询失败: {ex.Message}";
            MessageBox.Show($"查询交易历史失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在刷新数据...";

            await LoadTradeHistoriesAsync();

            StatusMessage = $"刷新完成，共 {TradeHistories.Count} 条记录";
        }
        catch (Exception ex)
        {
            StatusMessage = $"刷新失败: {ex.Message}";
            MessageBox.Show($"刷新数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV文件 (*.csv)|*.csv",
                FileName = $"交易历史_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.csv",
                DefaultExt = "csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                IsLoading = true;
                StatusMessage = "正在导出CSV文件...";

                var success = await _tradeHistoryService.ExportToCsvAsync(
                    saveFileDialog.FileName, StartDate, EndDate, 
                    string.IsNullOrWhiteSpace(FilterSymbol) ? null : FilterSymbol);

                if (success)
                {
                    StatusMessage = $"CSV文件导出成功: {saveFileDialog.FileName}";
                    MessageBox.Show($"CSV文件导出成功！\n文件路径: {saveFileDialog.FileName}", 
                        "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = "CSV文件导出失败";
                    MessageBox.Show("CSV文件导出失败，请检查文件路径和权限", 
                        "导出失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"导出失败: {ex.Message}";
            MessageBox.Show($"导出CSV失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region 私有方法

    private async Task LoadTradeHistoriesAsync()
    {
        try
        {
            var histories = await _tradeHistoryService.GetTradeHistoryAsync(
                StartDate, EndDate, 
                string.IsNullOrWhiteSpace(FilterSymbol) ? null : FilterSymbol);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TradeHistories.Clear();
                foreach (var history in histories)
                {
                    TradeHistories.Add(history);
                }
            });
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StatusMessage = $"加载数据失败: {ex.Message}";
            });
        }
    }

    #endregion
}
