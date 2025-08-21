using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderWatch.Models;
using OrderWatch.Services;
using OrderWatch.Views;
using OrderWatch.Utils;
using System.Collections.ObjectModel;
using System.Windows;

namespace OrderWatch.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IBinanceService _binanceService;
    private readonly IConfigService _configService;
    private readonly IConditionalOrderService _conditionalOrderService;
    private readonly ISymbolInfoService _symbolInfoService;
    private readonly ILogService _logService;
    private readonly Timer _refreshTimer;
    private readonly Timer _conditionalOrderTimer;

    public MainViewModel(IBinanceService binanceService, IConfigService configService, 
        IConditionalOrderService conditionalOrderService, ISymbolInfoService symbolInfoService)
    {
        _binanceService = binanceService;
        _configService = configService;
        _conditionalOrderService = conditionalOrderService;
        _symbolInfoService = symbolInfoService;
        _logService = new LogService();

        // 初始化集合
        Accounts = new ObservableCollection<AccountInfo>();
        Positions = new ObservableCollection<PositionInfo>();
        Orders = new ObservableCollection<OrderInfo>();
        ConditionalOrders = new ObservableCollection<ConditionalOrder>();
        CandidateSymbols = new ObservableCollection<CandidateSymbol>();

        // 初始化窗口标题（包含版本号）
        WindowTitle = VersionManager.GetFormattedVersion();

        // 初始化定时器
        _refreshTimer = new Timer(async _ => await RefreshDataAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        _conditionalOrderTimer = new Timer(async _ => await CheckConditionalOrdersAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        
        // 每天清理一次过期缓存和日志
        var cacheCleanupTimer = new Timer(async _ => await _symbolInfoService.ClearExpiredCacheAsync(), null, 
            TimeSpan.FromMinutes(1), TimeSpan.FromHours(24));
        var logCleanupTimer = new Timer(async _ => await _logService.CleanupExpiredLogsAsync(), null,
            TimeSpan.FromHours(1), TimeSpan.FromHours(24));

        // 加载初始数据
        _ = Task.Run(async () => await LoadInitialDataAsync());
        
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
    private decimal _conditionalStopLossRatio;

    // 窗口标题
    [ObservableProperty]
    private string _windowTitle = string.Empty;

    // 当前选中的合约信息
    [ObservableProperty]
    private string _selectedSymbolInfo = string.Empty;

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

            StatusMessage = $"数据已刷新 - {DateTime.Now:HH:mm:ss}";
            await _logService.LogInfoAsync("数据刷新完成", "System");
        }
        catch (Exception ex)
        {
            StatusMessage = $"刷新失败: {ex.Message}";
            await _logService.LogErrorAsync("数据刷新失败", ex, "System");
        }
        finally
        {
            IsLoading = false;
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
    private async Task RemoveCandidateSymbolAsync()
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
    private async Task PasteSymbolAsync()
    {
        try
        {
            // 从剪贴板读取文本
            if (Clipboard.ContainsText())
            {
                var clipboardText = Clipboard.GetText().Trim();
                
                // 检查是否是有效的合约名称（简单验证：包含USDT且长度合理）
                if (clipboardText.Contains("USDT") && clipboardText.Length >= 6 && clipboardText.Length <= 20)
                {
                    // 检查是否已存在
                    if (CandidateSymbols.Any(s => s.Symbol.Equals(clipboardText, StringComparison.OrdinalIgnoreCase)))
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            StatusMessage = $"合约 {clipboardText} 已存在于候选列表中";
                        });
                        await _logService.LogWarningAsync($"尝试添加已存在的合约: {clipboardText}", "System");
                        return;
                    }
                    
                    // 添加到候选列表
                    await _configService.AddCandidateSymbolAsync(clipboardText);
                    
                    var newSymbol = new CandidateSymbol
                    {
                        Symbol = clipboardText,
                        LatestPrice = 0,
                        PriceChangePercent = 0,
                        LastUpdateTime = DateTime.Now,
                        IsSelected = false
                    };
                    
                    // 使用Dispatcher更新UI
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        CandidateSymbols.Add(newSymbol);
                        StatusMessage = $"已从剪贴板添加合约: {clipboardText}";
                    });
                    
                    await _logService.LogInfoAsync($"从剪贴板添加合约: {clipboardText}", "System");
                }
                else
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusMessage = "剪贴板内容不是有效的合约名称";
                    });
                    await _logService.LogWarningAsync($"剪贴板内容无效: {clipboardText}", "System");
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

    #endregion

    #region 私有方法

    private async Task RefreshAccountInfoAsync()
    {
        if (SelectedAccount == null) return;

        try
        {
            var accountInfo = await _binanceService.GetAccountInfoAsync();
            if (accountInfo != null)
            {
                SelectedAccount.TotalWalletBalance = accountInfo.TotalWalletBalance;
                SelectedAccount.TotalUnrealizedProfit = accountInfo.TotalUnrealizedProfit;
                SelectedAccount.TotalMarginBalance = accountInfo.TotalMarginBalance;
                SelectedAccount.TotalPositionInitialMargin = accountInfo.TotalPositionInitialMargin;
                SelectedAccount.TotalOpenOrderInitialMargin = accountInfo.TotalOpenOrderInitialMargin;
                SelectedAccount.TotalInitialMargin = accountInfo.TotalInitialMargin;
                SelectedAccount.TotalMaintMargin = accountInfo.TotalMaintMargin;
                SelectedAccount.MaxWithdrawAmount = accountInfo.MaxWithdrawAmount;
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
            var positions = await _binanceService.GetPositionsAsync();
            
            Positions.Clear();
            foreach (var position in positions)
            {
                Positions.Add(position);
            }
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
            
            Orders.Clear();
            foreach (var order in orders)
            {
                Orders.Add(order);
            }
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
            
            ConditionalOrders.Clear();
            foreach (var order in conditionalOrders)
            {
                ConditionalOrders.Add(order);
            }
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
            await _conditionalOrderService.CheckAndExecuteConditionalOrdersAsync();
            await RefreshConditionalOrdersAsync();
        }
        catch (Exception ex)
        {
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
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await UpdateLatestPriceAsync();
                    });
                }
                catch (Exception ex)
                {
                    await _logService.LogErrorAsync($"OnSymbolChanged异常: {ex.Message}", ex, "System");
                }
            });
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
            await Application.Current.Dispatcher.InvokeAsync(async () =>
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
                    PriceChangePercent = symbolInfo.PriceChangePercent;
                    
                    // 更新状态栏显示的合约信息
                    SelectedSymbolInfo = $"{symbol} | 价格: {symbolInfo.LatestPrice:F4} | 24h: {symbolInfo.PriceChangePercent:F2}%";
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
                            SelectedSymbolInfo = $"{symbol} | 价格: {price:F4} | 备用方案";
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

    public void Dispose()
    {
        _refreshTimer?.Dispose();
        _conditionalOrderTimer?.Dispose();
        // 异步记录应用关闭日志，但不等待完成
        _ = Task.Run(async () => await _logService.LogInfoAsync("应用程序关闭", "System"));
    }
}
