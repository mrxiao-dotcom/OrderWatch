using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderWatch.Models;
using OrderWatch.Services;
using OrderWatch.Views;
using OrderWatch.Utils;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;


namespace OrderWatch.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IBinanceService _binanceService;
    private readonly IConfigService _configService;
    private readonly IConditionalOrderService _conditionalOrderService;
    private readonly ISymbolInfoService _symbolInfoService;
    private readonly IBinanceSymbolService _binanceSymbolService;
    private readonly ILogService _logService;
    private readonly ITradeHistoryService _tradeHistoryService;
    private readonly Timer _refreshTimer;
    private readonly Timer _conditionalOrderTimer;
    private readonly Timer _cacheCleanupTimer;
    private readonly Timer _logCleanupTimer;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public MainViewModel(IBinanceService binanceService, IConfigService configService, 
        IConditionalOrderService conditionalOrderService, ISymbolInfoService symbolInfoService, 
        IBinanceSymbolService binanceSymbolService)
    {
        _binanceService = binanceService;
        _configService = configService;
        _conditionalOrderService = conditionalOrderService;
        _symbolInfoService = symbolInfoService;
        _binanceSymbolService = binanceSymbolService;
        _logService = new LogService();
        _tradeHistoryService = new TradeHistoryService();

        // 初始化集合
        Accounts = new ObservableCollection<AccountInfo>();
        Positions = new ObservableCollection<PositionInfo>();
        Orders = new ObservableCollection<OrderInfo>();
        ConditionalOrders = new ObservableCollection<ConditionalOrder>();
        CandidateSymbols = new ObservableCollection<CandidateSymbol>();

        // 初始化窗口标题（包含版本号）
        WindowTitle = VersionManager.GetFormattedVersion();

        // 初始化定时器 - 使用CancellationTokenSource来控制取消
        _cancellationTokenSource = new CancellationTokenSource();
        
        _refreshTimer = new Timer(async _ => 
        {
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await RefreshDataAsync();
            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        
        _conditionalOrderTimer = new Timer(async _ => 
        {
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await CheckConditionalOrdersAsync();
            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        
        // 每天清理一次过期缓存和日志
        _cacheCleanupTimer = new Timer(async _ => 
        {
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await _symbolInfoService.ClearExpiredCacheAsync();
            }
        }, null, TimeSpan.FromMinutes(1), TimeSpan.FromHours(24));
        
        _logCleanupTimer = new Timer(async _ => 
        {
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await _logService.CleanupExpiredLogsAsync();
            }
        }, null, TimeSpan.FromHours(1), TimeSpan.FromHours(24));

        // 加载初始数据
        _ = Task.Run(async () => 
        {
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await LoadInitialDataAsync();
            }
        }, _cancellationTokenSource.Token);
        
        // 初始化币安合约服务
        _ = Task.Run(async () => 
        {
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await _binanceSymbolService.RefreshSymbolsCacheAsync();
            }
        }, _cancellationTokenSource.Token);
        
        // 记录应用启动日志
        _ = _logService.LogInfoAsync("应用程序启动", "System");
    }

    #region 属性

    [ObservableProperty]
    private AccountInfo? _selectedAccount;

    [ObservableProperty]
    private PositionInfo? _selectedPosition;

    [ObservableProperty]
    private OrderInfo? _selectedOrder;

    [ObservableProperty]
    private ConditionalOrder? _selectedConditionalOrder;

    [ObservableProperty]
    private CandidateSymbol? _selectedCandidateSymbol;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private bool _autoRefreshEnabled = true;

    [ObservableProperty]
    private string _symbol = string.Empty;

    [ObservableProperty]
    private string _side = "BUY";

    [ObservableProperty]
    private string _orderType = "MARKET";

    [ObservableProperty]
    private decimal _quantity;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private decimal _leverage = 10;

    [ObservableProperty]
    private string _newSymbolInput = string.Empty;

    [ObservableProperty]
    private List<string> _symbolSuggestions = new List<string>();

    [ObservableProperty]
    private decimal _stopLossRatio;

    [ObservableProperty]
    private decimal _latestPrice;

    [ObservableProperty]
    private decimal _priceChangePercent;

    // 市价下单区域属性
    [ObservableProperty]
    private string _marketSymbol = string.Empty;

    [ObservableProperty]
    private decimal _marketQuantity;

    [ObservableProperty]
    private string _marketSide = "BUY";

    [ObservableProperty]
    private decimal _marketLeverage = 10;

    [ObservableProperty]
    private int _riskTrades = 1; // 风险笔数，默认为1

    [ObservableProperty]
    private decimal _riskAmount; // 以损定量金额

    // 限价下单区域属性
    [ObservableProperty]
    private string _limitSymbol = string.Empty;

    [ObservableProperty]
    private decimal _limitPrice;

    [ObservableProperty]
    private decimal _limitQuantity;

    [ObservableProperty]
    private string _limitSide = "BUY";

    [ObservableProperty]
    private decimal _limitLeverage = 10;

    // 条件下单区域属性
    [ObservableProperty]
    private string _conditionalSymbol = string.Empty;

    [ObservableProperty]
    private decimal _conditionalTriggerPrice;

    [ObservableProperty]
    private decimal _conditionalQuantity;

    [ObservableProperty]
    private string _conditionalSide = "BUY";

    [ObservableProperty]
    private decimal _conditionalLeverage = 10;

    [ObservableProperty]
    private decimal _conditionalStopLossRatio = 10; // 默认止损比例10%

    // 条件单价格属性
    [ObservableProperty]
    private decimal _longBreakoutPrice;

    [ObservableProperty]
    private decimal _shortBreakdownPrice;

    // 价格偏差百分比属性
    [ObservableProperty]
    private string _limitPriceDeviation = string.Empty;

    [ObservableProperty]
    private string _longBreakoutPriceDeviation = string.Empty;

    [ObservableProperty]
    private string _shortBreakdownPriceDeviation = string.Empty;

    // 窗口标题
    [ObservableProperty]
    private string _windowTitle = string.Empty;

    // 当前选中的合约信息
    [ObservableProperty]
    private string _selectedSymbolInfo = string.Empty;
    
    // 最小精度要求信息
    [ObservableProperty]
    private string _minPrecisionInfo = string.Empty;

    #endregion

    #region 集合

    public ObservableCollection<AccountInfo> Accounts { get; }
    public ObservableCollection<PositionInfo> Positions { get; }
    public ObservableCollection<OrderInfo> Orders { get; }
    public ObservableCollection<ConditionalOrder> ConditionalOrders { get; }
    public ObservableCollection<CandidateSymbol> CandidateSymbols { get; }

    #endregion

    #region 命令

    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        if (!AutoRefreshEnabled || SelectedAccount == null) return;

        try
        {
            // 检查取消请求
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            IsLoading = true;
            StatusMessage = "正在刷新数据...";

            var tasks = new[]
            {
                RefreshAccountInfoAsync(),
                RefreshPositionsAsync(),
                RefreshOrdersAsync(),
                RefreshConditionalOrdersAsync()
            };

            await Task.WhenAll(tasks);

            // 再次检查取消请求
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            StatusMessage = $"数据已刷新 - {DateTime.Now:HH:mm:ss}";
            await _logService.LogInfoAsync("数据刷新完成", "System");
        }
        catch (OperationCanceledException)
        {
            // 操作被取消，静默处理
            return;
        }
        catch (Exception ex)
        {
            // 检查取消请求
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            StatusMessage = $"刷新失败: {ex.Message}";
            await _logService.LogErrorAsync("数据刷新失败", ex, "System");
        }
        finally
        {
            // 只有在没有被取消的情况下才更新UI
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task LoadInitialDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在加载初始数据...";

            // 加载账户配置
            var accounts = await _configService.LoadAccountsAsync();
            foreach (var account in accounts)
            {
                Accounts.Add(account);
            }

            // 加载候选币
            var symbols = await _configService.LoadCandidateSymbolsAsync();
            foreach (var symbol in symbols)
            {
                CandidateSymbols.Add(symbol);
            }

            // 加载条件单
            await RefreshConditionalOrdersAsync();

            StatusMessage = "初始数据加载完成";
            await _logService.LogInfoAsync($"初始数据加载完成，账户: {accounts.Count}，候选币: {symbols.Count}", "System");
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
            await _logService.LogErrorAsync("初始数据加载失败", ex, "System");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PlaceOrderAsync()
    {
        if (SelectedAccount == null || string.IsNullOrEmpty(Symbol) || Quantity <= 0)
        {
            StatusMessage = "请填写完整的下单信息";
            await _logService.LogWarningAsync("下单信息不完整", "Trading");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "正在下单...";

            // 设置API凭据
            _binanceService.SetCredentials(SelectedAccount.ApiKey, SelectedAccount.SecretKey, SelectedAccount.IsTestNet);

            // 创建交易请求
            var request = new TradingRequest
            {
                Symbol = Symbol,
                Side = Side,
                Type = OrderType,
                Quantity = Quantity,
                Price = OrderType == "LIMIT" ? Price : 0,
                StopPrice = 0,
                TimeInForce = OrderType == "LIMIT" ? "GTC" : "IOC",
                ReduceOnly = false,
                PositionSide = "LONG", // 可以根据需要调整
                WorkingType = "CONTRACT_PRICE",
                ClosePosition = false
            };

                          var success = await _binanceService.PlaceOrderAsync(request);
              if (success)
              {
                  StatusMessage = "下单成功！";
                  await _logService.LogTradeAsync(Symbol, $"下单成功 - {Side} {OrderType}", Price, Quantity, "成功");
                  
                  // 记录交易历史
                  await AddTradeHistoryAsync(Symbol, $"下单成功 - {Side} {OrderType}", Price, Quantity, "成功", OrderType, Side, Leverage, "Trading");
                  
                  // 如果设置了止损比例，创建止损单
                  if (StopLossRatio > 0)
                  {
                      await CreateStopLossOrderAsync(request);
                  }

                  // 刷新数据
                  await RefreshDataAsync();
              }
              else
              {
                  StatusMessage = "下单失败，请检查参数";
                  await _logService.LogTradeAsync(Symbol, $"下单失败 - {Side} {OrderType}", Price, Quantity, "失败");
                  
                  // 记录交易历史
                  await AddTradeHistoryAsync(Symbol, $"下单失败 - {Side} {OrderType}", Price, Quantity, "失败", OrderType, Side, Leverage, "Trading");
              }
        }
        catch (Exception ex)
        {
            StatusMessage = $"下单异常: {ex.Message}";
            await _logService.LogErrorAsync($"下单异常: {ex.Message}", ex, "Trading");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelOrderAsync()
    {
        if (SelectedOrder == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "正在取消订单...";

            var success = await _binanceService.CancelOrderAsync(SelectedOrder.Symbol, SelectedOrder.OrderId);
            if (success)
            {
                StatusMessage = "订单取消成功！";
                await _logService.LogTradeAsync(SelectedOrder.Symbol, "取消订单", SelectedOrder.Price, SelectedOrder.Quantity, "成功");
                await RefreshDataAsync();
            }
            else
            {
                StatusMessage = "订单取消失败";
                await _logService.LogTradeAsync(SelectedOrder.Symbol, "取消订单", SelectedOrder.Price, SelectedOrder.Quantity, "失败");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"取消异常: {ex.Message}";
            await _logService.LogErrorAsync($"取消订单异常: {ex.Message}", ex, "Trading");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddCandidateSymbolAsync()
    {
        // 如果有输入框的内容，优先使用输入框的内容
        if (!string.IsNullOrWhiteSpace(NewSymbolInput))
        {
            await AddSymbolFromInputAsync();
            return;
        }

        // 否则使用Symbol属性（兼容原有功能）
        if (string.IsNullOrEmpty(Symbol)) return;

        try
        {
            await _configService.AddCandidateSymbolAsync(Symbol);
            
            var newSymbol = new CandidateSymbol
            {
                Symbol = Symbol,
                LatestPrice = 0,
                PriceChangePercent = 0,
                LastUpdateTime = DateTime.Now,
                IsSelected = false
            };

            CandidateSymbols.Add(newSymbol);
            StatusMessage = $"已添加候选币: {Symbol}";
            await _logService.LogInfoAsync($"添加候选币: {Symbol}", "System");
        }
        catch (Exception ex)
        {
            StatusMessage = $"添加失败: {ex.Message}";
            await _logService.LogErrorAsync($"添加候选币失败: {ex.Message}", ex, "System");
        }
    }

    [RelayCommand]
    public async Task RemoveCandidateSymbolAsync()
    {
        if (SelectedCandidateSymbol == null) return;

        try
        {
            await _configService.RemoveCandidateSymbolAsync(SelectedCandidateSymbol.Symbol);
            CandidateSymbols.Remove(SelectedCandidateSymbol);
            StatusMessage = $"已删除候选币: {SelectedCandidateSymbol.Symbol}";
            await _logService.LogInfoAsync($"删除候选币: {SelectedCandidateSymbol.Symbol}", "System");
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败: {ex.Message}";
            await _logService.LogErrorAsync($"删除候选币失败: {ex.Message}", ex, "System");
        }
    }

    [RelayCommand]
    private async Task SelectCandidateSymbolAsync()
    {
        if (SelectedCandidateSymbol == null) return;

        Symbol = SelectedCandidateSymbol.Symbol;
        await UpdateLatestPriceAsync();
        StatusMessage = $"已选择: {SelectedCandidateSymbol.Symbol}";
        await _logService.LogInfoAsync($"选择候选币: {SelectedCandidateSymbol.Symbol}", "System");
    }

    [RelayCommand]
    private async Task AddAccountAsync()
    {
        try
        {
            var accountWindow = new AccountConfigWindow();
            if (accountWindow.ShowDialog() == true)
            {
                await _configService.SaveAccountAsync(accountWindow.AccountInfo);
                Accounts.Add(accountWindow.AccountInfo);
                StatusMessage = $"已添加账户: {accountWindow.AccountInfo.Name}";
                await _logService.LogInfoAsync($"添加账户: {accountWindow.AccountInfo.Name}", "System");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"添加账户失败: {ex.Message}";
            await _logService.LogErrorAsync($"添加账户失败: {ex.Message}", ex, "System");
        }
    }

    [RelayCommand]
    private async Task EditAccountAsync()
    {
        if (SelectedAccount == null) return;

        try
        {
            var accountWindow = new AccountConfigWindow(SelectedAccount);
            if (accountWindow.ShowDialog() == true)
            {
                await _configService.SaveAccountAsync(accountWindow.AccountInfo);
                StatusMessage = $"已更新账户: {accountWindow.AccountInfo.Name}";
                await _logService.LogInfoAsync($"更新账户: {accountWindow.AccountInfo.Name}", "System");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"编辑账户失败: {ex.Message}";
            await _logService.LogErrorAsync($"编辑账户失败: {ex.Message}", ex, "System");
        }
    }

    [RelayCommand]
    private async Task DeleteAccountAsync()
    {
        if (SelectedAccount == null) return;

        try
        {
            var result = MessageBox.Show($"确定要删除账户 '{SelectedAccount.Name}' 吗？", "确认删除", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                await _configService.DeleteAccountAsync(SelectedAccount.Name);
                Accounts.Remove(SelectedAccount);
                SelectedAccount = null;
                StatusMessage = "账户删除成功";
                await _logService.LogInfoAsync($"删除账户: {SelectedAccount?.Name}", "System");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除账户失败: {ex.Message}";
            await _logService.LogErrorAsync($"删除账户失败: {ex.Message}", ex, "System");
        }
    }

    // 条件单相关命令
    [RelayCommand]
    private async Task AddConditionalOrderAsync()
    {
        try
        {
            var conditionalOrderWindow = new ConditionalOrderWindow();
            if (conditionalOrderWindow.ShowDialog() == true)
            {
                var success = await _conditionalOrderService.CreateConditionalOrderAsync(conditionalOrderWindow.ConditionalOrder);
                if (success)
                {
                    ConditionalOrders.Add(conditionalOrderWindow.ConditionalOrder);
                    StatusMessage = $"已添加条件单: {conditionalOrderWindow.ConditionalOrder.Symbol}";
                    await _logService.LogTradeAsync(conditionalOrderWindow.ConditionalOrder.Symbol, "添加条件单", 
                        conditionalOrderWindow.ConditionalOrder.TriggerPrice, conditionalOrderWindow.ConditionalOrder.Quantity, "成功");
                }
                else
                {
                    StatusMessage = "添加条件单失败";
                    await _logService.LogTradeAsync(conditionalOrderWindow.ConditionalOrder.Symbol, "添加条件单", 
                        conditionalOrderWindow.ConditionalOrder.TriggerPrice, conditionalOrderWindow.ConditionalOrder.Quantity, "失败");
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"添加条件单失败: {ex.Message}";
            await _logService.LogErrorAsync($"添加条件单失败: {ex.Message}", ex, "Trading");
        }
    }

    [RelayCommand]
    private async Task EditConditionalOrderAsync()
    {
        if (SelectedConditionalOrder == null) return;

        try
        {
            var conditionalOrderWindow = new ConditionalOrderWindow(SelectedConditionalOrder);
            if (conditionalOrderWindow.ShowDialog() == true)
            {
                var success = await _conditionalOrderService.UpdateConditionalOrderAsync(conditionalOrderWindow.ConditionalOrder);
                if (success)
                {
                    StatusMessage = $"已更新条件单: {SelectedConditionalOrder.Symbol}";
                    await _logService.LogTradeAsync(SelectedConditionalOrder.Symbol, "更新条件单", 
                        SelectedConditionalOrder.TriggerPrice, SelectedConditionalOrder.Quantity, "成功");
                }
                else
                {
                    StatusMessage = "更新条件单失败";
                    await _logService.LogTradeAsync(SelectedConditionalOrder.Symbol, "更新条件单", 
                        SelectedConditionalOrder.TriggerPrice, SelectedConditionalOrder.Quantity, "失败");
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"编辑条件单失败: {ex.Message}";
            await _logService.LogErrorAsync($"编辑条件单失败: {ex.Message}", ex, "Trading");
        }
    }

    [RelayCommand]
    private async Task CancelConditionalOrderAsync()
    {
        if (SelectedConditionalOrder == null) return;

        try
        {
            var result = MessageBox.Show($"确定要取消条件单 '{SelectedConditionalOrder.Symbol}' 吗？", "确认取消", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                var success = await _conditionalOrderService.CancelConditionalOrderAsync(SelectedConditionalOrder.Id);
                if (success)
                {
                    StatusMessage = "条件单取消成功";
                    await _logService.LogTradeAsync(SelectedConditionalOrder.Symbol, "取消条件单", 
                        SelectedConditionalOrder.TriggerPrice, SelectedConditionalOrder.Quantity, "成功");
                    await RefreshConditionalOrdersAsync();
                }
                else
                {
                    StatusMessage = "条件单取消失败";
                    await _logService.LogTradeAsync(SelectedConditionalOrder.Symbol, "取消条件单", 
                        SelectedConditionalOrder.TriggerPrice, SelectedConditionalOrder.Quantity, "失败");
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"取消条件单失败: {ex.Message}";
            await _logService.LogErrorAsync($"取消条件单失败: {ex.Message}", ex, "Trading");
        }
    }

    [RelayCommand]
    private async Task DeleteConditionalOrderAsync()
    {
        if (SelectedConditionalOrder == null) return;

        try
        {
            var result = MessageBox.Show($"确定要删除条件单 '{SelectedConditionalOrder.Symbol}' 吗？", "确认删除", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                var success = await _conditionalOrderService.DeleteConditionalOrderAsync(SelectedConditionalOrder.Id);
                if (success)
                {
                    ConditionalOrders.Remove(SelectedConditionalOrder);
                    SelectedConditionalOrder = null;
                    StatusMessage = "条件单删除成功";
                    await _logService.LogTradeAsync(SelectedConditionalOrder?.Symbol ?? "", "删除条件单", null, null, "成功");
                }
                else
                {
                    StatusMessage = "条件单删除失败";
                    await _logService.LogTradeAsync(SelectedConditionalOrder.Symbol, "删除条件单", null, null, "失败");
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除条件单失败: {ex.Message}";
            await _logService.LogErrorAsync($"删除条件单失败: {ex.Message}", ex, "Trading");
        }
    }

    [RelayCommand]
    private async Task OpenLogViewerAsync()
    {
        try
        {
            var logViewerWindow = new LogViewerWindow();
            logViewerWindow.Show();
            await _logService.LogInfoAsync("打开日志查看器", "System");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"打开日志查看器失败: {ex.Message}", ex, "System");
        }
    }

    [RelayCommand]
    private async Task OpenTradeHistoryAsync()
    {
        try
        {
            var tradeHistoryWindow = new TradeHistoryWindow();
            tradeHistoryWindow.Show();
            await _logService.LogInfoAsync("打开交易历史查看器", "System");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"打开交易历史查看器失败: {ex.Message}", ex, "System");
        }
    }

    [RelayCommand]
    public async Task PasteSymbolAsync()
    {
        try
        {
            // 从剪贴板读取文本
            if (Clipboard.ContainsText())
            {
                var clipboardText = Clipboard.GetText().Trim();
                
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    // 将剪贴板内容填入输入框
                    NewSymbolInput = clipboardText;
                    
                    // 自动添加合约（使用新的验证和规范化逻辑）
                    await AddSymbolFromInputAsync();
                    
                    await _logService.LogInfoAsync($"从剪贴板粘贴并添加合约: {clipboardText}", "System");
                }
            }
            else
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "剪贴板中没有文本内容";
                });
            }
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StatusMessage = $"粘贴合约失败: {ex.Message}";
            });
            await _logService.LogErrorAsync($"粘贴合约失败: {ex.Message}", ex, "System");
        }
    }

    // 价格调整命令
    [RelayCommand]
    private void AdjustLimitPrice(object parameter)
    {
        if (parameter is string factorStr && decimal.TryParse(factorStr, out decimal factor))
        {
            // 如果限价单价格为0，使用最新价格作为基础
            decimal basePrice = LimitPrice > 0 ? LimitPrice : LatestPrice;
            if (basePrice > 0)
            {
                LimitPrice = Math.Round(basePrice * factor, 4);
                StatusMessage = $"限价单价格已调整: {LimitPrice:F4}";
                
                // 计算价格偏差百分比
                LimitPriceDeviation = CalculatePriceDeviation(LimitPrice, LatestPrice);
            }
            else
            {
                StatusMessage = "请先获取合约最新价格";
            }
        }
    }

    [RelayCommand]
    private void AdjustLongBreakoutPrice(object parameter)
    {
        if (parameter is string factorStr && decimal.TryParse(factorStr, out decimal factor))
        {
            // 如果突破价格为0，使用最新价格作为基础
            decimal basePrice = LongBreakoutPrice > 0 ? LongBreakoutPrice : LatestPrice;
            if (basePrice > 0)
            {
                LongBreakoutPrice = Math.Round(basePrice * factor, 4);
                StatusMessage = $"做多突破价格已调整: {LongBreakoutPrice:F4}";
                
                // 计算价格偏差百分比
                LongBreakoutPriceDeviation = CalculatePriceDeviation(LongBreakoutPrice, LatestPrice);
            }
            else
            {
                StatusMessage = "请先获取合约最新价格";
            }
        }
    }

    [RelayCommand]
    private void AdjustShortBreakdownPrice(object parameter)
    {
        if (parameter is string factorStr && decimal.TryParse(factorStr, out decimal factor))
        {
            // 如果跌破价格为0，使用最新价格作为基础
            decimal basePrice = ShortBreakdownPrice > 0 ? ShortBreakdownPrice : LatestPrice;
            if (basePrice > 0)
            {
                ShortBreakdownPrice = Math.Round(basePrice * factor, 4);
                StatusMessage = $"做空跌破价格已调整: {ShortBreakdownPrice:F4}";
                
                // 计算价格偏差百分比
                ShortBreakdownPriceDeviation = CalculatePriceDeviation(ShortBreakdownPrice, LatestPrice);
            }
            else
            {
                StatusMessage = "请先获取合约最新价格";
            }
        }
    }

         // 杠杆设置命令
     [RelayCommand]
     private void SetLeverage(object parameter)
     {
         if (parameter is string leverageStr && decimal.TryParse(leverageStr, out decimal leverage))
         {
             MarketLeverage = leverage;
             StatusMessage = $"杠杆已设置为: {leverage}x";
         }
     }

     // 止损比例设置命令
     [RelayCommand]
     private void SetStopLossRatio(object parameter)
     {
         if (parameter is string ratioStr && decimal.TryParse(ratioStr, out decimal ratio))
         {
             ConditionalStopLossRatio = ratio;
             StatusMessage = $"止损比例已设置为: {ratio}%";
             // 自动计算数量
             _ = Task.Run(async () => 
             {
                 if (!_cancellationTokenSource.Token.IsCancellationRequested)
                 {
                     await CalculateQuantityFromRiskAmountAsync();
                 }
             }, _cancellationTokenSource.Token);
         }
     }

    // 以损定量调整命令
    [RelayCommand]
    private void AdjustRiskAmount(object parameter)
    {
        if (parameter is string operation)
        {
            decimal currentAmount = RiskAmount;
            decimal baseAmount = GetBaseRiskAmount(); // 获取基础风险金额

            switch (operation)
            {
                case "double":
                    RiskAmount = Math.Max(1, Math.Round(currentAmount + baseAmount)); // 确保至少为1
                    StatusMessage = $"以损定量已加倍: {RiskAmount}";
                    break;
                case "addHalf":
                    RiskAmount = Math.Max(1, Math.Round(currentAmount + (baseAmount / 2))); // 确保至少为1
                    StatusMessage = $"以损定量已加半: {RiskAmount}";
                    break;
                case "half":
                    var halfAmount = Math.Round(currentAmount / 2);
                    RiskAmount = Math.Max(1, halfAmount); // 确保至少为1
                    StatusMessage = $"以损定量已减半: {RiskAmount}";
                    break;
            }

            // 自动计算数量
            _ = Task.Run(async () => 
            {
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await CalculateQuantityFromRiskAmountAsync();
                }
            }, _cancellationTokenSource.Token);
        }
    }

         // 市价下单命令
     [RelayCommand]
     private async Task PlaceMarketOrderAsync()
     {
         if (SelectedAccount == null || string.IsNullOrEmpty(MarketSymbol) || MarketQuantity <= 0)
         {
             StatusMessage = "请填写完整的市价下单信息";
             await _logService.LogWarningAsync("市价下单信息不完整", "Trading");
             return;
         }

         try
         {
             // 验证下单参数
             var (isValid, errorMessage) = await ValidateOrderParametersAsync(MarketSymbol, MarketQuantity, MarketLeverage);
             if (!isValid)
             {
                 StatusMessage = errorMessage;
                 await _logService.LogWarningAsync($"市价下单参数验证失败: {errorMessage}", "Trading");
                 return;
             }

             // 显示确认对话框
             var confirmMessage = $"确认市价下单？\n\n" +
                                $"合约: {MarketSymbol}\n" +
                                $"方向: {(MarketSide == "BUY" ? "买入" : "卖出")}\n" +
                                $"数量: {MarketQuantity:F4}\n" +
                                $"杠杆: {MarketLeverage}x\n" +
                                $"预估金额: {MarketQuantity * LatestPrice:F2} USDT";

             var result = await ShowConfirmDialogAsync(confirmMessage, "确认市价下单");
             if (result != true) return;

             IsLoading = true;
             StatusMessage = "正在执行市价下单...";

             // 设置API凭据
             _binanceService.SetCredentials(SelectedAccount.ApiKey, SelectedAccount.SecretKey, SelectedAccount.IsTestNet);

             // 创建市价下单请求
             var request = new TradingRequest
             {
                 Symbol = MarketSymbol,
                 Side = MarketSide,
                 Type = "MARKET",
                 Quantity = MarketQuantity,
                 Price = 0, // 市价单
                 StopPrice = 0,
                 TimeInForce = "IOC",
                 ReduceOnly = false,
                 PositionSide = MarketSide == "BUY" ? "LONG" : "SHORT",
                 WorkingType = "CONTRACT_PRICE",
                 ClosePosition = false
             };

             var success = await _binanceService.PlaceOrderAsync(request);
             if (success)
             {
                 StatusMessage = "市价下单成功！";
                 await _logService.LogTradeAsync(MarketSymbol, $"市价下单成功 - {MarketSide}", 0, MarketQuantity, "成功");
                 
                 // 刷新数据
                 await RefreshDataAsync();
             }
             else
             {
                 StatusMessage = "市价下单失败，请检查参数";
                 await _logService.LogTradeAsync(MarketSymbol, $"市价下单失败 - {MarketSide}", 0, MarketQuantity, "失败");
             }
         }
         catch (Exception ex)
         {
             StatusMessage = $"市价下单异常: {ex.Message}";
             await _logService.LogErrorAsync($"市价下单异常: {ex.Message}", ex, "Trading");
         }
         finally
         {
             IsLoading = false;
         }
     }

     // 限价委托下单命令
     [RelayCommand]
     private async Task PlaceLimitOrderAsync()
     {
         if (SelectedAccount == null || string.IsNullOrEmpty(MarketSymbol) || MarketQuantity <= 0 || LimitPrice <= 0)
         {
             StatusMessage = "请填写完整的限价委托信息";
             await _logService.LogWarningAsync("限价委托信息不完整", "Trading");
             return;
         }

         try
         {
             // 验证下单参数
             var (isValid, errorMessage) = await ValidateOrderParametersAsync(MarketSymbol, MarketQuantity, MarketLeverage);
             if (!isValid)
             {
                 StatusMessage = errorMessage;
                 await _logService.LogWarningAsync($"限价委托下单参数验证失败: {errorMessage}", "Trading");
                 return;
             }

             // 显示确认对话框
             var confirmMessage = $"确认限价委托下单？\n\n" +
                                $"合约: {MarketSymbol}\n" +
                                $"方向: {(MarketSide == "BUY" ? "买入" : "卖出")}\n" +
                                $"数量: {MarketQuantity:F4}\n" +
                                $"价格: {LimitPrice:F4}\n" +
                                $"杠杆: {MarketLeverage}x\n" +
                                $"预估金额: {MarketQuantity * LimitPrice:F2} USDT";

             var result = await ShowConfirmDialogAsync(confirmMessage, "确认限价委托下单");
             if (result != true) return;

             IsLoading = true;
             StatusMessage = "正在执行限价委托下单...";

             // 设置API凭据
             _binanceService.SetCredentials(SelectedAccount.ApiKey, SelectedAccount.SecretKey, SelectedAccount.IsTestNet);

             // 创建限价委托请求
             var request = new TradingRequest
             {
                 Symbol = MarketSymbol,
                 Side = MarketSide,
                 Type = "LIMIT",
                 Quantity = MarketQuantity,
                 Price = LimitPrice,
                 StopPrice = 0,
                 TimeInForce = "GTC",
                 ReduceOnly = false,
                 PositionSide = MarketSide == "BUY" ? "LONG" : "SHORT",
                 WorkingType = "CONTRACT_PRICE",
                 ClosePosition = false
             };

             var success = await _binanceService.PlaceOrderAsync(request);
             if (success)
             {
                 StatusMessage = "限价委托下单成功！";
                 await _logService.LogTradeAsync(MarketSymbol, $"限价委托下单成功 - {MarketSide}", LimitPrice, MarketQuantity, "成功");
                 
                 // 刷新数据
                 await RefreshDataAsync();
             }
             else
             {
                 StatusMessage = "限价委托下单失败，请检查参数";
                 await _logService.LogTradeAsync(MarketSymbol, $"限价委托下单失败 - {MarketSide}", LimitPrice, MarketQuantity, "失败");
             }
         }
         catch (Exception ex)
         {
             StatusMessage = $"限价委托下单异常: {ex.Message}";
             await _logService.LogErrorAsync($"限价委托下单异常: {ex.Message}", ex, "Trading");
         }
         finally
         {
             IsLoading = false;
         }
     }

     // 做多条件单命令
     [RelayCommand]
     private async Task AddLongConditionalOrderAsync()
     {
         if (SelectedAccount == null || string.IsNullOrEmpty(MarketSymbol) || MarketQuantity <= 0 || LongBreakoutPrice <= 0)
         {
             StatusMessage = "请填写完整的做多条件单信息";
             await _logService.LogWarningAsync("做多条件单信息不完整", "Trading");
             return;
         }

         try
         {
             // 显示确认对话框
             var confirmMessage = $"确认设置做多条件单？\n\n" +
                                $"合约: {MarketSymbol}\n" +
                                $"方向: 买入\n" +
                                $"数量: {MarketQuantity:F4}\n" +
                                $"触发价格: {LongBreakoutPrice:F4}\n" +
                                $"杠杆: {MarketLeverage}x\n" +
                                $"预估金额: {MarketQuantity * LongBreakoutPrice:F2} USDT";

             var result = await ShowConfirmDialogAsync(confirmMessage, "确认做多条件单");
             if (result != true) return;

             IsLoading = true;
             StatusMessage = "正在设置做多条件单...";

             // 创建做多条件单
             var conditionalOrder = new ConditionalOrder
             {
                 Symbol = MarketSymbol,
                 Side = "BUY",
                 Quantity = MarketQuantity,
                 TriggerPrice = LongBreakoutPrice,
                 OrderType = "LIMIT",
                 Price = LongBreakoutPrice, // 触发时的委托价格
                 Leverage = MarketLeverage,
                 Status = "PENDING",
                 CreatedTime = DateTime.Now
             };

             var success = await _conditionalOrderService.CreateConditionalOrderAsync(conditionalOrder);
             if (success)
             {
                 StatusMessage = "做多条件单设置成功！";
                 await _logService.LogTradeAsync(MarketSymbol, "设置做多条件单", LongBreakoutPrice, MarketQuantity, "成功");
                 
                 // 刷新条件单列表
                 await RefreshConditionalOrdersAsync();
             }
             else
             {
                 StatusMessage = "做多条件单设置失败";
                 await _logService.LogTradeAsync(MarketSymbol, "设置做多条件单", LongBreakoutPrice, MarketQuantity, "失败");
             }
         }
         catch (Exception ex)
         {
             StatusMessage = $"做多条件单设置异常: {ex.Message}";
             await _logService.LogErrorAsync($"做多条件单设置异常: {ex.Message}", ex, "Trading");
         }
         finally
         {
             IsLoading = false;
         }
     }

     // 做空条件单命令
     [RelayCommand]
     private async Task AddShortConditionalOrderAsync()
     {
         if (SelectedAccount == null || string.IsNullOrEmpty(MarketSymbol) || MarketQuantity <= 0 || ShortBreakdownPrice <= 0)
         {
             StatusMessage = "请填写完整的做空条件单信息";
             await _logService.LogWarningAsync("做空条件单信息不完整", "Trading");
             return;
         }

         try
         {
             // 显示确认对话框
             var confirmMessage = $"确认设置做空条件单？\n\n" +
                                $"合约: {MarketSymbol}\n" +
                                $"方向: 卖出\n" +
                                $"数量: {MarketQuantity:F4}\n" +
                                $"触发价格: {ShortBreakdownPrice:F4}\n" +
                                $"杠杆: {MarketLeverage}x\n" +
                                $"预估金额: {MarketQuantity * ShortBreakdownPrice:F2} USDT";

             var result = await ShowConfirmDialogAsync(confirmMessage, "确认做空条件单");
             if (result != true) return;

             IsLoading = true;
             StatusMessage = "正在设置做空条件单...";

             // 创建做空条件单
             var conditionalOrder = new ConditionalOrder
             {
                 Symbol = MarketSymbol,
                 Side = "SELL",
                 Quantity = MarketQuantity,
                 TriggerPrice = ShortBreakdownPrice,
                 OrderType = "LIMIT",
                 Price = ShortBreakdownPrice, // 触发时的委托价格
                 Leverage = MarketLeverage,
                 Status = "PENDING",
                 CreatedTime = DateTime.Now
             };

             var success = await _conditionalOrderService.CreateConditionalOrderAsync(conditionalOrder);
             if (success)
             {
                 StatusMessage = "做空条件单设置成功！";
                 await _logService.LogTradeAsync(MarketSymbol, "设置做空条件单", ShortBreakdownPrice, MarketQuantity, "成功");
                 
                 // 刷新条件单列表
                 await RefreshConditionalOrdersAsync();
             }
             else
             {
                 StatusMessage = "做空条件单设置失败";
                 await _logService.LogTradeAsync(MarketSymbol, "设置做空条件单", ShortBreakdownPrice, MarketQuantity, "失败");
             }
         }
         catch (Exception ex)
         {
             StatusMessage = $"做空条件单设置异常: {ex.Message}";
             await _logService.LogErrorAsync($"做空条件单设置异常: {ex.Message}", ex, "Trading");
         }
         finally
         {
             IsLoading = false;
         }
     }

         // 保盈加仓命令
    [RelayCommand]
    private async Task PlaceProfitOrderAsync()
    {
        if (SelectedAccount == null || string.IsNullOrEmpty(MarketSymbol) || MarketQuantity <= 0)
        {
            StatusMessage = "请填写完整的下单信息";
            await _logService.LogWarningAsync("保盈加仓信息不完整", "Trading");
            return;
        }

        try
        {
            // 检查是否有对应持仓
            var position = Positions?.FirstOrDefault(p => p.Symbol == MarketSymbol);
            if (position == null)
            {
                StatusMessage = "没有找到对应的持仓，无法执行保盈加仓";
                await _logService.LogWarningAsync($"保盈加仓失败：没有找到合约 {MarketSymbol} 的持仓", "Trading");
                return;
            }

            // 检查浮盈是否大于50%的风险金
            decimal minProfitRequired = RiskAmount * 0.5m; // 至少50%的风险金
            if (position.UnRealizedProfit < minProfitRequired)
            {
                StatusMessage = $"浮盈不足，需要至少 {minProfitRequired:F2} USDT，当前浮盈: {position.UnRealizedProfit:F2} USDT";
                await _logService.LogWarningAsync($"保盈加仓失败：浮盈不足，需要 {minProfitRequired:F2}，当前 {position.UnRealizedProfit:F2}", "Trading");
                return;
            }

            // 确定下单方向（与持仓方向一致）
            string orderSide = position.IsLong ? "BUY" : "SELL";
            string positionSide = position.IsLong ? "LONG" : "SHORT";

            // 显示确认对话框
            var confirmMessage = $"确认保盈加仓？\n\n" +
                               $"合约: {MarketSymbol}\n" +
                               $"方向: {(orderSide == "BUY" ? "买入" : "卖出")} ({positionSide})\n" +
                               $"数量: {MarketQuantity:F4}\n" +
                               $"当前浮盈: {position.UnRealizedProfit:F2} USDT\n" +
                               $"入场价: {position.EntryPrice:F4}\n" +
                               $"最新价: {position.MarkPrice:F4}";

            var result = await ShowConfirmDialogAsync(confirmMessage, "确认保盈加仓");
            if (result != true) return;

            IsLoading = true;
            StatusMessage = "正在执行保盈加仓...";

            // 设置API凭据
            _binanceService.SetCredentials(SelectedAccount.ApiKey, SelectedAccount.SecretKey, SelectedAccount.IsTestNet);

            // 1. 执行保盈加仓市价单
            var profitOrderRequest = new TradingRequest
            {
                Symbol = MarketSymbol,
                Side = orderSide,
                Type = "MARKET",
                Quantity = MarketQuantity,
                Price = 0, // 市价单
                StopPrice = 0,
                TimeInForce = "IOC",
                ReduceOnly = false,
                PositionSide = positionSide,
                WorkingType = "CONTRACT_PRICE",
                ClosePosition = false
            };

            var profitOrderSuccess = await _binanceService.PlaceOrderAsync(profitOrderRequest);
            if (!profitOrderSuccess)
            {
                StatusMessage = "保盈加仓下单失败，请检查参数";
                await _logService.LogErrorAsync("保盈加仓下单失败", null, "Trading");
                return;
            }

            StatusMessage = "保盈加仓下单成功，正在设置止损单...";

            // 2. 取消之前的止损委托
            await CancelExistingStopLossOrdersAsync(MarketSymbol);

            // 3. 以下入场价为触发价设置新的止损委托
            var stopLossRequest = new TradingRequest
            {
                Symbol = MarketSymbol,
                Side = orderSide == "BUY" ? "SELL" : "BUY", // 止损方向与持仓相反
                Type = "STOP_MARKET",
                Quantity = MarketQuantity,
                Price = 0, // 市价止损
                StopPrice = position.EntryPrice, // 以入场价为触发价
                TimeInForce = "GTC",
                ReduceOnly = true, // 止损单必须是减仓单
                PositionSide = positionSide,
                WorkingType = "CONTRACT_PRICE",
                ClosePosition = false
            };

            var stopLossSuccess = await _binanceService.PlaceOrderAsync(stopLossRequest);
            if (stopLossSuccess)
            {
                StatusMessage = "保盈加仓成功！已设置止损单，触发价: 入场价";
                await _logService.LogTradeAsync(MarketSymbol, $"保盈加仓成功 - {orderSide}", 0, MarketQuantity, "成功");
                await _logService.LogTradeAsync(MarketSymbol, "设置止损单", position.EntryPrice, MarketQuantity, "成功");
            }
            else
            {
                StatusMessage = "保盈加仓成功，但止损单设置失败";
                await _logService.LogTradeAsync(MarketSymbol, $"保盈加仓成功 - {orderSide}", 0, MarketQuantity, "成功");
                await _logService.LogWarningAsync("止损单设置失败", "Trading");
            }

            // 刷新数据
            await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"保盈加仓异常: {ex.Message}";
            await _logService.LogErrorAsync($"保盈加仓异常: {ex.Message}", ex, "Trading");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 取消指定合约的现有止损委托
    /// </summary>
    private async Task CancelExistingStopLossOrdersAsync(string symbol)
    {
        try
        {
            // 获取当前所有开放委托
            var openOrders = await _binanceService.GetOpenOrdersAsync();
            if (openOrders == null || !openOrders.Any()) return;

            // 过滤出指定合约的止损委托
            var stopLossOrders = openOrders.Where(o => 
                o.Symbol == symbol && 
                (o.Type == "STOP_MARKET" || o.Type == "STOP_LIMIT" || 
                 o.Type == "STOP" || o.Type == "STOP_LOSS"));

            foreach (var order in stopLossOrders)
            {
                try
                {
                    await _binanceService.CancelOrderAsync(symbol, order.OrderId);
                    await _logService.LogInfoAsync($"取消止损委托: {order.OrderId}", "Trading");
                }
                catch (Exception ex)
                {
                    await _logService.LogWarningAsync($"取消止损委托失败: {order.OrderId}, 错误: {ex.Message}", "Trading");
                }
            }
        }
        catch (Exception ex)
        {
            await _logService.LogWarningAsync($"获取开放委托失败: {ex.Message}", "Trading");
        }
    }

    #endregion

    #region 私有方法

    private async Task RefreshAccountInfoAsync()
    {
        if (SelectedAccount == null) return;

        try
        {
            var accountInfo = await _binanceService.GetDetailedAccountInfoAsync();
            if (accountInfo != null)
            {
                // 更新基本账户信息
                SelectedAccount.TotalWalletBalance = accountInfo.TotalWalletBalance;
                SelectedAccount.TotalUnrealizedProfit = accountInfo.TotalUnrealizedProfit;
                SelectedAccount.TotalMarginBalance = accountInfo.TotalMarginBalance;
                SelectedAccount.TotalPositionInitialMargin = accountInfo.TotalPositionInitialMargin;
                SelectedAccount.TotalOpenOrderInitialMargin = accountInfo.TotalOpenOrderInitialMargin;
                SelectedAccount.TotalInitialMargin = accountInfo.TotalInitialMargin;
                SelectedAccount.TotalMaintMargin = accountInfo.TotalMaintMargin;
                SelectedAccount.MaxWithdrawAmount = accountInfo.MaxWithdrawAmount;
                
                // 更新新增的市值和杠杆信息
                SelectedAccount.LongMarketValue = accountInfo.LongMarketValue;
                SelectedAccount.ShortMarketValue = accountInfo.ShortMarketValue;
                SelectedAccount.TotalMarketValue = accountInfo.TotalMarketValue;
                SelectedAccount.NetMarketValue = accountInfo.NetMarketValue;
                SelectedAccount.Leverage = accountInfo.Leverage;
                
                await _logService.LogInfoAsync($"账户信息刷新成功 - 权益: {SelectedAccount.TotalEquity:F2}, 杠杆: {SelectedAccount.Leverage:F2}", "API");
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync("刷新账户信息失败", ex, "API");
        }
    }

    private async Task RefreshPositionsAsync()
    {
        if (SelectedAccount == null) return;

        try
        {
            var positions = await _binanceService.GetDetailedPositionsAsync();
            
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Positions.Clear();
                foreach (var position in positions)
                {
                    Positions.Add(position);
                }
            });
            
            await _logService.LogInfoAsync($"持仓信息刷新成功，共 {positions.Count} 个持仓", "API");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync("刷新持仓信息失败", ex, "API");
        }
    }

    private async Task RefreshOrdersAsync()
    {
        if (SelectedAccount == null) return;

        try
        {
            var orders = await _binanceService.GetOpenOrdersAsync();
            
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Orders.Clear();
                foreach (var order in orders)
                {
                    Orders.Add(order);
                }
            });
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync("刷新订单信息失败", ex, "API");
        }
    }

    private async Task RefreshConditionalOrdersAsync()
    {
        try
        {
            var conditionalOrders = await _conditionalOrderService.GetConditionalOrdersAsync();
            
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ConditionalOrders.Clear();
                foreach (var order in conditionalOrders)
                {
                    ConditionalOrders.Add(order);
                }
            });
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync("刷新条件单信息失败", ex, "System");
        }
    }

    private async Task CheckConditionalOrdersAsync()
    {
        try
        {
            // 检查取消请求
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            await _conditionalOrderService.CheckAndExecuteConditionalOrdersAsync();
            
            // 再次检查取消请求
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            await RefreshConditionalOrdersAsync();
        }
        catch (OperationCanceledException)
        {
            // 操作被取消，静默处理
            return;
        }
        catch (Exception ex)
        {
            // 检查取消请求
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            await _logService.LogErrorAsync("检查条件单失败", ex, "System");
        }
    }

    private async Task UpdateLatestPriceAsync()
    {
        if (string.IsNullOrEmpty(Symbol)) return;

        try
        {
            var price = await _binanceService.GetLatestPriceAsync(Symbol);
            if (price > 0)
            {
                LatestPrice = price;
                
                // 如果是限价单，自动填入价格
                if (OrderType == "LIMIT")
                {
                    Price = price;
                }
            }

            var changePercent = await _binanceService.Get24hrPriceChangeAsync(Symbol);
            PriceChangePercent = changePercent;
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"更新最新价格失败: {Symbol}", ex, "API");
        }
    }

    private async Task CreateStopLossOrderAsync(TradingRequest originalRequest)
    {
        try
        {
            var stopLossPrice = CalculateStopLossPrice(LatestPrice, StopLossRatio, Side);
            
            var stopRequest = new TradingRequest
            {
                Symbol = originalRequest.Symbol,
                Side = Side == "BUY" ? "SELL" : "BUY",
                Type = "STOP_MARKET",
                Quantity = originalRequest.Quantity,
                StopPrice = stopLossPrice,
                ReduceOnly = true,
                PositionSide = originalRequest.PositionSide,
                WorkingType = "CONTRACT_PRICE",
                ClosePosition = false
            };

            var success = await _binanceService.PlaceOrderAsync(stopRequest);
            if (success)
            {
                StatusMessage = $"止损单创建成功，止损价: {stopLossPrice:F4}";
                await _logService.LogTradeAsync(originalRequest.Symbol, "创建止损单", stopLossPrice, originalRequest.Quantity, "成功");
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"创建止损单失败: {originalRequest.Symbol}", ex, "Trading");
        }
    }

    private decimal CalculateStopLossPrice(decimal currentPrice, decimal stopLossRatio, string side)
    {
        if (side == "BUY")
        {
            return currentPrice * (1 - stopLossRatio / 100);
        }
        else
        {
            return currentPrice * (1 + stopLossRatio / 100);
        }
    }

    #endregion

    #region 属性变化处理

    partial void OnSymbolChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            // 使用Dispatcher确保UI更新在正确的线程上执行
            _ = Task.Run(async () => 
            {
                try
                {
                    if (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            if (!_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                await UpdateLatestPriceAsync();
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    if (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await _logService.LogErrorAsync($"OnSymbolChanged异常: {ex.Message}", ex, "System");
                    }
                }
            }, _cancellationTokenSource.Token);
        }
    }

    partial void OnOrderTypeChanged(string value)
    {
        // 如果是限价单，自动填入最新价格
        if (value == "LIMIT" && LatestPrice > 0)
        {
            Price = LatestPrice;
        }
    }

    partial void OnNewSymbolInputChanged(string value)
    {
        // 当输入变化时，异步获取合约建议
        _ = Task.Run(async () => 
        {
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await OnNewSymbolInputChangedAsync();
            }
        }, _cancellationTokenSource.Token);
    }

    partial void OnConditionalStopLossRatioChanged(decimal value)
    {
        // 当止损比例变化时，自动计算数量
        if (value > 0 && RiskAmount > 0 && LatestPrice > 0)
        {
            _ = Task.Run(async () => 
            {
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await CalculateQuantityFromRiskAmountAsync();
                }
            }, _cancellationTokenSource.Token);
        }
    }

    partial void OnRiskAmountChanged(decimal value)
    {
        // 验证以损定量输入：必须为正整数
        if (value <= 0)
        {
            // 如果输入小于等于0，重置为默认值
            RiskAmount = GetBaseRiskAmount();
            StatusMessage = "以损定量必须为正数，已重置为默认值";
            return;
        }
        
        // 如果输入不是整数，四舍五入为整数
        if (value != Math.Floor(value))
        {
            var roundedValue = Math.Round(value);
            RiskAmount = roundedValue;
            StatusMessage = $"以损定量已四舍五入为整数: {roundedValue}";
            return;
        }
        
        // 当以损定量变化时，自动计算数量
        if (value > 0 && ConditionalStopLossRatio > 0 && LatestPrice > 0)
        {
            _ = Task.Run(async () => 
            {
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await CalculateQuantityFromRiskAmountAsync();
                }
            }, _cancellationTokenSource.Token);
        }
    }

    partial void OnLimitPriceChanged(decimal value)
    {
        // 当限价单价格变化时，自动计算偏差百分比
        if (value > 0 && LatestPrice > 0)
        {
            LimitPriceDeviation = CalculatePriceDeviation(value, LatestPrice);
        }
        else
        {
            LimitPriceDeviation = string.Empty;
        }
    }

    partial void OnLongBreakoutPriceChanged(decimal value)
    {
        // 当做多突破价格变化时，自动计算偏差百分比
        if (value > 0 && LatestPrice > 0)
        {
            LongBreakoutPriceDeviation = CalculatePriceDeviation(value, LatestPrice);
        }
        else
        {
            LongBreakoutPriceDeviation = string.Empty;
        }
    }

    partial void OnShortBreakdownPriceChanged(decimal value)
    {
        // 当做空跌破价格变化时，自动计算偏差百分比
        if (value > 0 && LatestPrice > 0)
        {
            ShortBreakdownPriceDeviation = CalculatePriceDeviation(value, LatestPrice);
        }
        else
        {
            ShortBreakdownPriceDeviation = string.Empty;
        }
    }

    #endregion

    #region 命令

    [RelayCommand]
    public async Task AddSymbolFromInputAsync()
    {
        if (string.IsNullOrWhiteSpace(NewSymbolInput))
            return;

        try
        {
            // 使用新的币安合约服务进行验证和规范化
            var normalizedSymbol = await _binanceSymbolService.NormalizeSymbolAsync(NewSymbolInput.Trim());
            
            // 验证合约是否可交易
            if (!await _binanceSymbolService.IsSymbolTradableAsync(normalizedSymbol))
            {
                // 获取建议列表
                var suggestions = await _binanceSymbolService.GetSymbolSuggestionsAsync(NewSymbolInput.Trim(), 5);
                
                var errorMessage = $"合约 '{NewSymbolInput.Trim()}' 不存在或无法获取价格信息。\n\n";
                
                if (suggestions.Any())
                {
                    errorMessage += "可能的合约名称：\n";
                    foreach (var suggestion in suggestions)
                    {
                        errorMessage += $"• {suggestion}\n";
                    }
                    errorMessage += "\n请选择正确的合约名称。";
                }
                else
                {
                    errorMessage += "请检查合约名称是否正确，或确认该合约是否在币安期货交易中可用。";
                }
                
                await ShowErrorDialogAsync(errorMessage);
                return;
            }

            // 检查是否已经存在
            if (CandidateSymbols.Any(s => s.Symbol.Equals(normalizedSymbol, StringComparison.OrdinalIgnoreCase)))
            {
                await ShowErrorDialogAsync($"合约 '{normalizedSymbol}' 已经存在于候选列表中。");
                return;
            }

            // 添加到候选列表
            var newCandidate = new CandidateSymbol { Symbol = normalizedSymbol };
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                CandidateSymbols.Add(newCandidate);
            });
            
            // 自动保存到本地文件
            await _configService.AddCandidateSymbolAsync(normalizedSymbol);
            
            // 清空输入框和建议列表
            NewSymbolInput = string.Empty;
            SymbolSuggestions.Clear();
            
            // 记录日志
            await _logService.LogInfoAsync($"手动添加合约到候选列表: {normalizedSymbol}", "User");
            
            // 更新状态
            StatusMessage = $"已添加合约: {normalizedSymbol}";
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"添加合约失败: {NewSymbolInput}", ex, "User");
            await ShowErrorDialogAsync($"添加合约失败: {ex.Message}");
        }
    }



    [RelayCommand]
    private async Task OnNewSymbolInputChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(NewSymbolInput))
        {
            SymbolSuggestions.Clear();
            return;
        }

        try
        {
            // 获取合约建议
            var suggestions = await _binanceSymbolService.GetSymbolSuggestionsAsync(NewSymbolInput.Trim(), 8);
            SymbolSuggestions = suggestions;
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"获取合约建议失败: {NewSymbolInput}", ex, "User");
            SymbolSuggestions.Clear();
        }
    }

    #endregion

    #region 平仓相关命令

    [RelayCommand]
    private async Task ClosePositionAsync()
    {
        if (SelectedPosition == null)
        {
            await ShowErrorDialogAsync("请先选择一个持仓进行平仓操作。");
            return;
        }

        try
        {
            // 停止自动刷新
            AutoRefreshEnabled = false;
            StatusMessage = "已停止自动刷新，准备平仓操作...";

            // 确认平仓
            var confirmMessage = $"确认平仓？\n\n" +
                               $"合约: {SelectedPosition.Symbol}\n" +
                               $"方向: {SelectedPosition.PositionSideDisplay}\n" +
                               $"数量: {SelectedPosition.PositionAmt:F4}\n" +
                               $"进场价: {SelectedPosition.EntryPrice:F4}\n" +
                               $"当前盈亏: {SelectedPosition.UnRealizedProfit:F2}\n\n" +
                               $"平仓后将删除该合约的所有委托单和条件单。";

            var result = await ShowConfirmDialogAsync(confirmMessage, "确认平仓");
            if (result != true)
            {
                // 恢复自动刷新
                AutoRefreshEnabled = true;
                StatusMessage = "平仓操作已取消";
                return;
            }

            StatusMessage = "正在执行平仓操作...";

            // 1. 市价平仓
            var closeSide = SelectedPosition.IsLong ? "SELL" : "BUY";
            var closeRequest = new TradingRequest
            {
                Symbol = SelectedPosition.Symbol,
                Side = closeSide,
                Type = "MARKET",
                Quantity = Math.Abs(SelectedPosition.PositionAmt)
            };

            var closeSuccess = await _binanceService.PlaceOrderAsync(closeRequest);
            if (!closeSuccess)
            {
                throw new Exception("市价平仓失败");
            }

            // 2. 删除该合约的所有委托单
            await CancelAllOpenOrdersForSymbolAsync(SelectedPosition.Symbol);

            // 3. 删除该合约的所有条件单
            await CancelAllConditionalOrdersForSymbolAsync(SelectedPosition.Symbol);

            StatusMessage = $"平仓成功！已删除 {SelectedPosition.Symbol} 的所有委托单和条件单";

            // 记录交易历史
            await AddTradeHistoryAsync(SelectedPosition.Symbol, "平仓", 0, Math.Abs(SelectedPosition.PositionAmt), 
                "成功", "MARKET", closeSide, SelectedPosition.Leverage, "Position Close");

            // 恢复自动刷新
            AutoRefreshEnabled = true;

            // 刷新数据
            await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"平仓失败: {ex.Message}", ex, "Trading");
            await ShowErrorDialogAsync($"平仓失败: {ex.Message}");
            
            // 恢复自动刷新
            AutoRefreshEnabled = true;
            StatusMessage = $"平仓失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CloseAllPositionsAsync()
    {
        if (!Positions.Any())
        {
            await ShowErrorDialogAsync("当前没有持仓，无需平仓。");
            return;
        }

        try
        {
            // 停止自动刷新
            AutoRefreshEnabled = false;
            StatusMessage = "已停止自动刷新，准备一键全平操作...";

            // 确认一键全平
            var confirmMessage = $"确认一键全平？\n\n" +
                               $"当前持仓数量: {Positions.Count}\n" +
                               $"总持仓价值: {Positions.Sum(p => p.Notional):F2}\n\n" +
                               $"此操作将平掉所有持仓，并删除所有委托单和条件单。\n" +
                               $"操作不可撤销！";

            var result = await ShowConfirmDialogAsync(confirmMessage, "确认一键全平");
            if (result != true)
            {
                // 恢复自动刷新
                AutoRefreshEnabled = true;
                StatusMessage = "一键全平操作已取消";
                return;
            }

            StatusMessage = "正在执行一键全平操作...";

            var successCount = 0;
            var failCount = 0;

            // 逐个平仓
            foreach (var position in Positions.ToList())
            {
                try
                {
                    var closeSide = position.IsLong ? "SELL" : "BUY";
                    var closeRequest = new TradingRequest
                    {
                        Symbol = position.Symbol,
                        Side = closeSide,
                        Type = "MARKET",
                        Quantity = Math.Abs(position.PositionAmt)
                    };

                    var closeSuccess = await _binanceService.PlaceOrderAsync(closeRequest);
                    if (closeSuccess)
                    {
                        successCount++;
                        
                        // 记录交易历史
                        await AddTradeHistoryAsync(position.Symbol, "一键全平", 0, Math.Abs(position.PositionAmt), 
                            "成功", "MARKET", closeSide, position.Leverage, "Position Close");
                    }
                    else
                    {
                        failCount++;
                        await _logService.LogWarningAsync($"平仓失败: {position.Symbol}", "Trading");
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    await _logService.LogErrorAsync($"平仓异常: {position.Symbol} - {ex.Message}", ex, "Trading");
                }
            }

            // 删除所有委托单和条件单
            await CancelAllOpenOrdersAsync();
            await CancelAllConditionalOrdersAsync();

            StatusMessage = $"一键全平完成！成功: {successCount}, 失败: {failCount}";

            // 恢复自动刷新
            AutoRefreshEnabled = true;

            // 刷新数据
            await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"一键全平失败: {ex.Message}", ex, "Trading");
            await ShowErrorDialogAsync($"一键全平失败: {ex.Message}");
            
            // 恢复自动刷新
            AutoRefreshEnabled = true;
            StatusMessage = $"一键全平失败: {ex.Message}";
        }
    }

    #endregion

    #region 自动填写功能

    /// <summary>
    /// 自动填写合约名称到所有下单区，并获取合约信息
    /// </summary>
    public async Task AutoFillSymbolToOrderAreasAsync(string symbol)
    {
        if (string.IsNullOrEmpty(symbol)) return;

        try
        {
            await _logService.LogInfoAsync($"开始自动填写合约: {symbol}", "System");

            // 使用Dispatcher确保UI更新在正确的线程上执行
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 填写到市价下单区
                MarketSymbol = symbol;
                
                // 填写到限价下单区
                LimitSymbol = symbol;
                
                // 填写到条件下单区
                ConditionalSymbol = symbol;
            });

            // 获取合约信息（包含最新价格）
            var symbolInfo = await _symbolInfoService.GetSymbolInfoAsync(symbol);
            if (symbolInfo != null)
            {
                await _logService.LogInfoAsync($"成功获取合约信息: {symbol} = {symbolInfo.LatestPrice}", "API");
                
                // 使用Dispatcher更新UI属性
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                                         // 更新所有价格字段
                     LatestPrice = symbolInfo.LatestPrice;
                     LimitPrice = symbolInfo.LatestPrice;
                     ConditionalTriggerPrice = symbolInfo.LatestPrice;
                     LongBreakoutPrice = symbolInfo.LatestPrice;
                     ShortBreakdownPrice = symbolInfo.LatestPrice;
                     PriceChangePercent = symbolInfo.PriceChangePercent;
                     
                     // 计算价格偏差百分比
                     LimitPriceDeviation = CalculatePriceDeviation(LimitPrice, LatestPrice);
                     LongBreakoutPriceDeviation = CalculatePriceDeviation(LongBreakoutPrice, LatestPrice);
                     ShortBreakdownPriceDeviation = CalculatePriceDeviation(ShortBreakdownPrice, LatestPrice);
                    
                    // 更新状态栏显示的合约信息
                    SelectedSymbolInfo = $"{symbol} | 价格: {symbolInfo.LatestPrice:F4} | 24h: {symbolInfo.PriceChangePercent:F2}%";
                    
                    // 自动计算以损定量（账户权益除以风险笔数）
                    if (SelectedAccount?.TotalEquity > 0)
                    {
                        var baseAmount = GetBaseRiskAmount();
                        RiskAmount = Math.Max(1, Math.Round(baseAmount)); // 确保至少为1
                    }
                    
                    // 自动计算数量
                    if (ConditionalStopLossRatio > 0)
                    {
                        _ = Task.Run(async () => 
                        {
                            if (!_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                await CalculateQuantityFromRiskAmountAsync();
                            }
                        }, _cancellationTokenSource.Token);
                    }
                    
                    // 设置最小精度要求信息
                    _ = Task.Run(async () => 
                    {
                        if (!_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            MinPrecisionInfo = await GetMinPrecisionInfoAsync(symbol);
                        }
                    }, _cancellationTokenSource.Token);
                });
            }
            else
            {
                await _logService.LogWarningAsync($"获取合约信息失败: {symbol}", "API");
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SelectedSymbolInfo = $"{symbol} | 获取信息失败";
                });
                
                // 尝试直接从BinanceService获取价格作为备用方案
                try
                {
                    var price = await _binanceService.GetLatestPriceAsync(symbol);
                    if (price > 0)
                    {
                                                 await Application.Current.Dispatcher.InvokeAsync(() =>
                         {
                             LatestPrice = price;
                             LimitPrice = price;
                             ConditionalTriggerPrice = price;
                             LongBreakoutPrice = price;
                             ShortBreakdownPrice = price;
                             SelectedSymbolInfo = $"{symbol} | 价格: {price:F4} | 备用方案";
                             
                             // 计算价格偏差百分比
                             LimitPriceDeviation = CalculatePriceDeviation(LimitPrice, LatestPrice);
                             LongBreakoutPriceDeviation = CalculatePriceDeviation(LongBreakoutPrice, LatestPrice);
                             ShortBreakdownPriceDeviation = CalculatePriceDeviation(ShortBreakdownPrice, LatestPrice);
                            
                            // 自动计算以损定量（账户权益除以风险笔数）
                            if (SelectedAccount?.TotalEquity > 0)
                            {
                                var baseAmount = GetBaseRiskAmount();
                                RiskAmount = Math.Max(1, Math.Round(baseAmount)); // 确保至少为1
                            }
                            
                            // 自动计算数量
                            if (ConditionalStopLossRatio > 0)
                            {
                                _ = Task.Run(async () => 
                                {
                                    if (!_cancellationTokenSource.Token.IsCancellationRequested)
                                    {
                                        await CalculateQuantityFromRiskAmountAsync();
                                    }
                                }, _cancellationTokenSource.Token);
                            }
                            
                            // 设置最小精度要求信息
                            _ = Task.Run(async () => 
                            {
                                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                                {
                                    MinPrecisionInfo = await GetMinPrecisionInfoAsync(symbol);
                                }
                            }, _cancellationTokenSource.Token);
                        });
                        
                        await _logService.LogInfoAsync($"备用方案成功获取价格: {symbol} = {price}", "API");
                    }
                    else
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            SelectedSymbolInfo = $"{symbol} | 价格获取失败";
                        });
                        await _logService.LogWarningAsync($"备用方案也失败: {symbol} 价格为0", "API");
                    }
                }
                catch (Exception ex)
                {
                    await _logService.LogErrorAsync($"备用方案异常: {symbol}", ex, "API");
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SelectedSymbolInfo = $"{symbol} | 价格获取异常: {ex.Message}";
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"AutoFillSymbolToOrderAreasAsync异常: {symbol}", ex, "System");
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SelectedSymbolInfo = $"{symbol} | 处理异常: {ex.Message}";
            });
        }
    }

    #endregion

    #region 持仓自动填写功能

    /// <summary>
    /// 自动填写持仓信息到下单区
    /// </summary>
    public async Task AutoFillPositionToOrderAreasAsync(PositionInfo position)
    {
        if (position == null) return;

        try
        {
            await _logService.LogInfoAsync($"开始自动填写持仓信息: {position.Symbol}", "System");

            // 使用Dispatcher确保UI更新在正确的线程上执行
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 填写到市价下单区
                MarketSymbol = position.Symbol;
                
                // 填写到限价下单区
                LimitSymbol = position.Symbol;
                
                // 填写到条件下单区
                ConditionalSymbol = position.Symbol;

                // 填写杠杆
                MarketLeverage = position.Leverage;
            });

            // 获取合约信息（包含最新价格）
            var symbolInfo = await _symbolInfoService.GetSymbolInfoAsync(position.Symbol);
            if (symbolInfo != null)
            {
                await _logService.LogInfoAsync($"成功获取合约信息: {position.Symbol} = {symbolInfo.LatestPrice}", "API");
                
                // 使用Dispatcher更新UI属性
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 更新所有价格字段
                    LatestPrice = symbolInfo.LatestPrice;
                    LimitPrice = symbolInfo.LatestPrice;
                    ConditionalTriggerPrice = symbolInfo.LatestPrice;
                    LongBreakoutPrice = symbolInfo.LatestPrice;
                    ShortBreakdownPrice = symbolInfo.LatestPrice;
                    PriceChangePercent = symbolInfo.PriceChangePercent;
                    
                    // 计算价格偏差百分比
                    LimitPriceDeviation = CalculatePriceDeviation(LimitPrice, LatestPrice);
                    LongBreakoutPriceDeviation = CalculatePriceDeviation(LongBreakoutPrice, LatestPrice);
                    ShortBreakdownPriceDeviation = CalculatePriceDeviation(ShortBreakdownPrice, LatestPrice);
                    
                    // 更新状态栏显示的合约信息
                    SelectedSymbolInfo = $"{position.Symbol} | 价格: {symbolInfo.LatestPrice:F4} | 24h: {symbolInfo.PriceChangePercent:F2}% | 持仓: {position.PositionSideDisplay}";
                    
                    // 自动计算以损定量（账户权益除以风险笔数）
                    if (SelectedAccount?.TotalEquity > 0)
                    {
                        var baseAmount = GetBaseRiskAmount();
                        RiskAmount = Math.Max(1, Math.Round(baseAmount)); // 确保至少为1
                    }
                    
                    // 自动计算数量
                    if (ConditionalStopLossRatio > 0)
                    {
                        _ = Task.Run(async () => 
                        {
                            if (!_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                await CalculateQuantityFromRiskAmountAsync();
                            }
                        }, _cancellationTokenSource.Token);
                    }
                    
                    // 设置最小精度要求信息
                    _ = Task.Run(async () => 
                    {
                        if (!_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            MinPrecisionInfo = await GetMinPrecisionInfoAsync(position.Symbol);
                        }
                    }, _cancellationTokenSource.Token);
                });
            }
            else
            {
                await _logService.LogWarningAsync($"获取合约信息失败: {position.Symbol}", "API");
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SelectedSymbolInfo = $"{position.Symbol} | 持仓: {position.PositionSideDisplay} | 获取信息失败";
                });
                
                // 尝试直接从BinanceService获取价格作为备用方案
                try
                {
                    var price = await _binanceService.GetLatestPriceAsync(position.Symbol);
                    if (price > 0)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            LatestPrice = price;
                            LimitPrice = price;
                            ConditionalTriggerPrice = price;
                            LongBreakoutPrice = price;
                            ShortBreakdownPrice = price;
                            SelectedSymbolInfo = $"{position.Symbol} | 持仓: {position.PositionSideDisplay} | 价格: {price:F4} | 备用方案";
                            
                            // 计算价格偏差百分比
                            LimitPriceDeviation = CalculatePriceDeviation(LimitPrice, LatestPrice);
                            LongBreakoutPriceDeviation = CalculatePriceDeviation(LongBreakoutPrice, LatestPrice);
                            ShortBreakdownPriceDeviation = CalculatePriceDeviation(ShortBreakdownPrice, LatestPrice);
                            
                            // 自动计算以损定量（账户权益除以风险笔数）
                            if (SelectedAccount?.TotalEquity > 0)
                            {
                                var baseAmount = GetBaseRiskAmount();
                                RiskAmount = Math.Max(1, Math.Round(baseAmount)); // 确保至少为1
                            }
                            
                            // 自动计算数量
                            if (ConditionalStopLossRatio > 0)
                            {
                                _ = Task.Run(async () => 
                                {
                                    if (!_cancellationTokenSource.Token.IsCancellationRequested)
                                    {
                                        await CalculateQuantityFromRiskAmountAsync();
                                    }
                                }, _cancellationTokenSource.Token);
                            }
                            
                            // 设置最小精度要求信息
                            _ = Task.Run(async () => 
                            {
                                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                                {
                                    MinPrecisionInfo = await GetMinPrecisionInfoAsync(position.Symbol);
                                }
                            }, _cancellationTokenSource.Token);
                        });
                        
                        await _logService.LogInfoAsync($"备用方案成功获取价格: {position.Symbol} = {price}", "API");
                    }
                    else
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            SelectedSymbolInfo = $"{position.Symbol} | 持仓: {position.PositionSideDisplay} | 价格获取失败";
                        });
                        await _logService.LogWarningAsync($"备用方案也失败: {position.Symbol} 价格为0", "API");
                    }
                }
                catch (Exception ex)
                {
                    await _logService.LogErrorAsync($"备用方案异常: {position.Symbol}", ex, "API");
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SelectedSymbolInfo = $"{position.Symbol} | 持仓: {position.PositionSideDisplay} | 价格获取异常: {ex.Message}";
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"AutoFillPositionToOrderAreasAsync异常: {position.Symbol}", ex, "System");
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SelectedSymbolInfo = $"{position.Symbol} | 持仓: {position.PositionSideDisplay} | 处理异常: {ex.Message}";
            });
        }
    }

    #endregion

    #region 平仓辅助方法

    /// <summary>
    /// 取消指定合约的所有委托单
    /// </summary>
    private async Task CancelAllOpenOrdersForSymbolAsync(string symbol)
    {
        try
        {
            var openOrders = await _binanceService.GetOpenOrdersAsync();
            var symbolOrders = openOrders.Where(o => o.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)).ToList();
            
            foreach (var order in symbolOrders)
            {
                try
                {
                    var cancelSuccess = await _binanceService.CancelOrderAsync(order.Symbol, order.OrderId);
                    if (cancelSuccess)
                    {
                        await _logService.LogInfoAsync($"成功取消委托单: {order.Symbol} {order.OrderId}", "Trading");
                    }
                    else
                    {
                        await _logService.LogWarningAsync($"取消委托单失败: {order.Symbol} {order.OrderId}", "Trading");
                    }
                }
                catch (Exception ex)
                {
                    await _logService.LogErrorAsync($"取消委托单异常: {order.Symbol} {order.OrderId}", ex, "Trading");
                }
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"获取委托单失败: {symbol}", ex, "Trading");
        }
    }

    /// <summary>
    /// 取消指定合约的所有条件单
    /// </summary>
    private async Task CancelAllConditionalOrdersForSymbolAsync(string symbol)
    {
        try
        {
            var conditionalOrders = await _conditionalOrderService.GetConditionalOrdersAsync();
            var symbolConditionalOrders = conditionalOrders.Where(o => o.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)).ToList();
            
            foreach (var conditionalOrder in symbolConditionalOrders)
            {
                try
                {
                    var cancelSuccess = await _conditionalOrderService.CancelConditionalOrderAsync(conditionalOrder.Id);
                    if (cancelSuccess)
                    {
                        await _logService.LogInfoAsync($"成功取消条件单: {conditionalOrder.Symbol} {conditionalOrder.Id}", "Trading");
                    }
                    else
                    {
                        await _logService.LogWarningAsync($"取消条件单失败: {conditionalOrder.Symbol} {conditionalOrder.Id}", "Trading");
                    }
                }
                catch (Exception ex)
                {
                    await _logService.LogErrorAsync($"取消条件单异常: {conditionalOrder.Symbol} {conditionalOrder.Id}", ex, "Trading");
                }
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"获取条件单失败: {symbol}", ex, "Trading");
        }
    }

    /// <summary>
    /// 取消所有委托单
    /// </summary>
    private async Task CancelAllOpenOrdersAsync()
    {
        try
        {
            var openOrders = await _binanceService.GetOpenOrdersAsync();
            
            foreach (var order in openOrders)
            {
                try
                {
                    var cancelSuccess = await _binanceService.CancelOrderAsync(order.Symbol, order.OrderId);
                    if (cancelSuccess)
                    {
                        await _logService.LogInfoAsync($"成功取消委托单: {order.Symbol} {order.OrderId}", "Trading");
                    }
                    else
                    {
                        await _logService.LogWarningAsync($"取消委托单失败: {order.Symbol} {order.OrderId}", "Trading");
                    }
                }
                catch (Exception ex)
                {
                    await _logService.LogErrorAsync($"取消委托单异常: {order.Symbol} {order.OrderId}", ex, "Trading");
                }
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"获取委托单失败", ex, "Trading");
        }
    }

    /// <summary>
    /// 取消所有条件单
    /// </summary>
    private async Task CancelAllConditionalOrdersAsync()
    {
        try
        {
            var conditionalOrders = await _conditionalOrderService.GetConditionalOrdersAsync();
            
            foreach (var conditionalOrder in conditionalOrders)
            {
                try
                {
                    var cancelSuccess = await _conditionalOrderService.CancelConditionalOrderAsync(conditionalOrder.Id);
                    if (cancelSuccess)
                    {
                        await _logService.LogInfoAsync($"成功取消条件单: {conditionalOrder.Symbol} {conditionalOrder.Id}", "Trading");
                    }
                    else
                    {
                        await _logService.LogWarningAsync($"取消条件单失败: {conditionalOrder.Symbol} {conditionalOrder.Id}", "Trading");
                    }
                }
                catch (Exception ex)
                {
                    await _logService.LogErrorAsync($"取消条件单异常: {conditionalOrder.Symbol} {conditionalOrder.Id}", ex, "Trading");
                }
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"获取条件单失败", ex, "Trading");
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 规范化合约名称
    /// </summary>
    private string NormalizeSymbol(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Trim().ToUpper();
        
        // 常见的合约后缀
        var commonSuffixes = new[] { "USDT", "BUSD", "BTC", "ETH" };
        
        // 如果输入的是基础币种（如BTC、ETH），自动添加USDT后缀
        if (normalized.Length <= 4 && !commonSuffixes.Any(s => normalized.EndsWith(s)))
        {
            // 检查是否是常见的基础币种
            var baseCoins = new[] { "BTC", "ETH", "BNB", "ADA", "DOT", "LINK", "LTC", "BCH", "XRP", "EOS" };
            if (baseCoins.Contains(normalized))
            {
                normalized += "USDT";
            }
        }
        
        // 如果输入的是小写，转换为大写
        if (input.All(c => char.IsLower(c) || char.IsDigit(c)))
        {
            normalized = input.ToUpper();
            // 同样检查是否需要添加后缀
            if (normalized.Length <= 4 && !commonSuffixes.Any(s => normalized.EndsWith(s)))
            {
                var baseCoins = new[] { "BTC", "ETH", "BNB", "ADA", "DOT", "LINK", "LTC", "BCH", "XRP", "EOS" };
                if (baseCoins.Contains(normalized))
                {
                    normalized += "USDT";
                }
            }
        }
        
        return normalized;
    }

         /// <summary>
     /// 显示错误对话框
     /// </summary>
     private async Task ShowErrorDialogAsync(string message)
     {
         await Application.Current.Dispatcher.InvokeAsync(() =>
         {
             MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
         });
     }

     /// <summary>
     /// 显示确认对话框
     /// </summary>
     private async Task<bool?> ShowConfirmDialogAsync(string message, string title)
     {
         return await Application.Current.Dispatcher.InvokeAsync(() =>
         {
             var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
             return result == MessageBoxResult.Yes ? true : false;
         });
     }

    /// <summary>
    /// 获取基础风险金额（账户权益除以风险次数）
    /// </summary>
    private decimal GetBaseRiskAmount()
    {
        if (SelectedAccount?.TotalEquity > 0)
        {
            return Math.Floor(SelectedAccount.TotalEquity / SelectedAccount.RiskCapitalTimes);
        }
        return 100; // 默认值
    }

    /// <summary>
    /// 根据以损定量自动计算数量
    /// </summary>
    private async Task CalculateQuantityFromRiskAmountAsync()
    {
        if (RiskAmount <= 0 || ConditionalStopLossRatio <= 0 || LatestPrice <= 0)
            return;

        try
        {
            // 计算货值：货值 = 以损定量金 / 止损比例
            decimal notionalValue = RiskAmount / (ConditionalStopLossRatio / 100);
            
            // 计算数量：货值 / 最新价
            decimal rawQuantity = notionalValue / LatestPrice;
            
            // 标准化数量（根据币安要求调整精度）
            decimal standardizedQuantity = await StandardizeQuantityAsync(MarketSymbol, rawQuantity);
            
            // 更新数量
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MarketQuantity = standardizedQuantity;
                LimitQuantity = standardizedQuantity;
            });

            StatusMessage = $"数量已自动计算: {standardizedQuantity:F4}";
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"计算数量失败: {ex.Message}", ex, "System");
        }
    }

         /// <summary>
     /// 标准化数量（根据币安要求调整精度）
     /// </summary>
     private async Task<decimal> StandardizeQuantityAsync(string symbol, decimal quantity)
     {
         try
         {
             if (string.IsNullOrEmpty(symbol) || quantity <= 0)
                 return quantity;

             // 获取合约信息以获取精度要求
             var symbolInfo = await _symbolInfoService.GetSymbolInfoAsync(symbol);
             if (symbolInfo != null && symbolInfo.QuantityPrecision > 0)
             {
                 // 使用合约的实际数量精度
                 var precision = symbolInfo.QuantityPrecision;
                 var step = 1.0m / (decimal)Math.Pow(10, precision);
                 
                 // 计算标准化后的数量
                 var standardizedQuantity = Math.Round(quantity / step) * step;
                 
                 // 确保数量不小于最小下单数量
                 if (symbolInfo.MinQuantity > 0 && standardizedQuantity < symbolInfo.MinQuantity)
                 {
                     standardizedQuantity = symbolInfo.MinQuantity;
                 }
                 
                 await _logService.LogInfoAsync($"数量标准化: {symbol} {quantity} -> {standardizedQuantity} (精度: {precision})", "Trading");
                 return standardizedQuantity;
             }
             
             // 如果无法获取合约信息，使用智能推断的精度
             var inferredPrecision = GetInferredQuantityPrecision(symbol);
             var inferredStep = 1.0m / (decimal)Math.Pow(10, inferredPrecision);
             var inferredQuantity = Math.Round(quantity / inferredStep) * inferredStep;
             
             await _logService.LogInfoAsync($"数量标准化(推断): {symbol} {quantity} -> {inferredQuantity} (推断精度: {inferredPrecision})", "Trading");
             return inferredQuantity;
         }
         catch (Exception ex)
         {
             await _logService.LogErrorAsync($"标准化数量失败: {ex.Message}", ex, "System");
             // 降级处理：使用简单四舍五入
             return Math.Round(quantity, 4);
         }
     }

     /// <summary>
     /// 根据合约名称智能推断数量精度
     /// </summary>
     private int GetInferredQuantityPrecision(string symbol)
     {
         if (string.IsNullOrEmpty(symbol))
             return 4;
             
         var upperSymbol = symbol.ToUpper();
         
         // 根据合约名称推断精度要求
         if (upperSymbol.Contains("BTC") || upperSymbol.Contains("ETH") || upperSymbol.Contains("BNB"))
         {
             return 3; // 0.001
         }
         else if (upperSymbol.Contains("USDT") || upperSymbol.Contains("BUSD"))
         {
             return 1; // 0.1
         }
         else if (upperSymbol.Contains("DOGE") || upperSymbol.Contains("SHIB"))
         {
             return 0; // 1 (整数)
         }
         else if (upperSymbol.Contains("ADA") || upperSymbol.Contains("DOT") || upperSymbol.Contains("LINK"))
         {
             return 2; // 0.01
         }
         else
         {
             return 1; // 默认 0.1
         }
     }

     /// <summary>
     /// 验证下单参数
     /// </summary>
     private async Task<(bool isValid, string errorMessage)> ValidateOrderParametersAsync(string symbol, decimal quantity, decimal leverage)
     {
         try
         {
             // 基本验证
             if (string.IsNullOrEmpty(symbol))
                 return (false, "合约名称不能为空");

             if (quantity <= 0)
                 return (false, "数量必须大于0");

             if (leverage <= 0)
                 return (false, "杠杆必须大于0");

             // 数量精度验证
             var standardizedQuantity = await StandardizeQuantityAsync(symbol, quantity);
             if (standardizedQuantity != quantity)
             {
                 await Application.Current.Dispatcher.InvokeAsync(() =>
                 {
                     MarketQuantity = standardizedQuantity;
                 });
                 await _logService.LogInfoAsync($"数量已标准化: {quantity} -> {standardizedQuantity}", "Trading");
             }

             // 杠杆限制验证
             if (leverage > 125)
             {
                 return (false, "杠杆不能超过125倍");
             }

             // 市值限制验证（这里需要根据币安的具体限制调整）
             var estimatedValue = quantity * LatestPrice;
             if (estimatedValue < 5) // 最小5 USDT
             {
                 return (false, "预估市值不能少于5 USDT");
             }

             if (estimatedValue > 1000000) // 最大100万 USDT
             {
                 return (false, "预估市值不能超过100万 USDT");
             }

             return (true, string.Empty);
         }
         catch (Exception ex)
         {
             await _logService.LogErrorAsync($"验证下单参数失败: {ex.Message}", ex, "Trading");
             return (false, $"验证失败: {ex.Message}");
         }
     }

    /// <summary>
    /// 获取合约的最小精度要求信息
    /// </summary>
    private async Task<string> GetMinPrecisionInfoAsync(string symbol)
    {
        try
        {
            if (string.IsNullOrEmpty(symbol))
                return string.Empty;
                
            // 尝试从合约信息获取实际精度
            var symbolInfo = await _symbolInfoService.GetSymbolInfoAsync(symbol);
            if (symbolInfo != null && symbolInfo.QuantityPrecision >= 0)
            {
                var precision = symbolInfo.QuantityPrecision;
                var step = 1.0m / (decimal)Math.Pow(10, precision);
                var minQuantity = symbolInfo.MinQuantity > 0 ? symbolInfo.MinQuantity : step;
                
                return $"(精度: {step:F6}, 最小: {minQuantity:F6})";
            }
            
            // 如果无法获取合约信息，使用智能推断的精度
            var inferredPrecision = GetInferredQuantityPrecision(symbol);
            var inferredStep = 1.0m / (decimal)Math.Pow(10, inferredPrecision);
            
            return $"(推断精度: {inferredStep:F6})";
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"获取精度信息失败: {ex.Message}", ex, "System");
            return "(精度: 未知)";
        }
    }
    
    /// <summary>
    /// 计算价格偏差百分比
    /// </summary>
    private string CalculatePriceDeviation(decimal currentPrice, decimal latestPrice)
    {
        if (latestPrice <= 0 || currentPrice <= 0)
            return string.Empty;

        var deviation = ((currentPrice - latestPrice) / latestPrice) * 100;
        var sign = deviation >= 0 ? "+" : "";
        return $"{sign}{deviation:F2}%";
    }

    #endregion

    #region 交易历史记录

    /// <summary>
    /// 添加交易历史记录
    /// </summary>
    private async Task AddTradeHistoryAsync(string symbol, string action, decimal? price, decimal? quantity, 
        string result, string orderType, string side, decimal leverage, string category, string details = "")
    {
        try
        {
            var tradeHistory = new TradeHistory
            {
                Symbol = symbol,
                Action = action,
                Price = price,
                Quantity = quantity,
                Result = result,
                OrderType = orderType,
                Side = side,
                Leverage = leverage,
                Category = category,
                Details = details
            };

            await _tradeHistoryService.AddTradeHistoryAsync(tradeHistory);
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"记录交易历史失败: {ex.Message}", ex, "System");
        }
    }

    #endregion

    /// <summary>
    /// 强制退出，清理所有资源
    /// </summary>
    public void ForceExit()
    {
        try
        {
            // 取消所有异步操作
            _cancellationTokenSource?.Cancel();
            
            // 停止自动刷新
            AutoRefreshEnabled = false;
            
            // 等待一小段时间让异步操作完成
            Thread.Sleep(200);
            
            // 强制释放资源
            Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ForceExit异常: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            Console.WriteLine("=== 开始释放 MainViewModel 资源 ===");
            
            // 取消所有异步操作
            _cancellationTokenSource?.Cancel();
            Console.WriteLine("已取消异步操作");
            
            // 停止并释放所有定时器
            if (_refreshTimer != null)
            {
                _refreshTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _refreshTimer.Dispose();
                Console.WriteLine("已释放刷新定时器");
            }
            
            if (_conditionalOrderTimer != null)
            {
                _conditionalOrderTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _conditionalOrderTimer.Dispose();
                Console.WriteLine("已释放条件单定时器");
            }
            
            if (_cacheCleanupTimer != null)
            {
                _cacheCleanupTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _cacheCleanupTimer.Dispose();
                Console.WriteLine("已释放缓存清理定时器");
            }
            
            if (_logCleanupTimer != null)
            {
                _logCleanupTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _logCleanupTimer.Dispose();
                Console.WriteLine("已释放日志清理定时器");
            }
            
            // 释放CancellationTokenSource
            _cancellationTokenSource?.Dispose();
            Console.WriteLine("已释放取消令牌源");
            
            // 释放其他资源
            _binanceSymbolService?.Dispose();
            Console.WriteLine("已释放币安符号服务");
            
            // 强制垃圾回收
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Console.WriteLine("已执行垃圾回收");
            
            Console.WriteLine("=== MainViewModel 资源释放完成 ===");
        }
        catch (Exception ex)
        {
            // 静默处理Dispose异常，避免影响程序退出
            Console.WriteLine($"Dispose异常: {ex.Message}");
        }
    }
}
