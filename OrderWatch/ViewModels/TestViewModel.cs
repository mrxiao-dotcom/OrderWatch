using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderWatch.Models;
using OrderWatch.Services;
using OrderWatch.Utils;
using System.Collections.ObjectModel;
using System.Windows;
using System.Threading.Tasks;

namespace OrderWatch.ViewModels;

/// <summary>
/// 简化的测试ViewModel - 包含限价单、做多做空条件单功能
/// </summary>
public partial class TestViewModel : ObservableObject
{
    private readonly IConfigService? _configService;
    private readonly ILogService? _logService;
    private readonly IBinanceService? _binanceService;
    private readonly IBinanceSymbolService? _binanceSymbolService;
    
    // 自动刷新相关
    private System.Threading.Timer? _refreshTimer;
    private readonly SemaphoreSlim _refreshSemaphore = new SemaphoreSlim(1, 1);

    public TestViewModel()
    {
        // 从version.json读取版本号并设置窗口标题
        WindowTitle = VersionManager.GetFormattedVersion();
        
        // 初始化服务
        try
        {
            _configService = new ConfigService();
            _logService = new LogService();
            _binanceService = new BinanceService();
            _binanceSymbolService = new BinanceSymbolService();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TestViewModel服务初始化失败: {ex.Message}");
        }

        // 初始化集合
        Accounts = new ObservableCollection<AccountInfo>();
        Positions = new ObservableCollection<PositionInfo>();
        Orders = new ObservableCollection<OrderInfo>();
        OpenOrders = new ObservableCollection<OrderInfo>();
        ConditionalOrders = new ObservableCollection<ConditionalOrder>();
        CandidateSymbols = new ObservableCollection<CandidateSymbol>();
        SymbolSuggestions = new ObservableCollection<string>();

        // 初始化命令
        InitializeCommands();
        
        // 异步加载已保存的账户，如果没有则添加模拟数据
        _ = Task.Run(async () => await LoadInitialDataAsync());
        
        // 启动自动刷新（默认启用）
        if (AutoRefreshEnabled)
        {
            StartAutoRefresh();
        }
    }

    #region 属性

    public string WindowTitle { get; }

    private AccountInfo? _selectedAccount;
    public AccountInfo? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            if (SetProperty(ref _selectedAccount, value))
            {
                                 // 当切换账户时，设置API凭据并自动刷新账户信息
                 _ = Task.Run(async () =>
                 {
                     try
                     {
                         // 设置BinanceService的API凭据
                         if (_binanceService != null && value != null)
                         {
                             _binanceService.SetCredentials(value.ApiKey, value.SecretKey, value.IsTestNet);
                             
                             await Application.Current.Dispatcher.InvokeAsync(() =>
                             {
                                 StatusMessage = $"正在连接账户: {value.Name}...";
                             });
                             
                             // 测试连接
                             bool isConnected = await _binanceService.TestConnectionAsync();
                             if (isConnected)
                             {
                                 await Application.Current.Dispatcher.InvokeAsync(() =>
                                 {
                                     StatusMessage = $"✅ 账户连接成功，正在获取信息...";
                                 });
                                 
                                 // 刷新账户和持仓信息
                                 await RefreshAccountInfoAsync();
                                 await RefreshPositionsDataAsync();
                             }
                             else
                             {
                                 await Application.Current.Dispatcher.InvokeAsync(() =>
                                 {
                                     StatusMessage = $"❌ 账户连接失败，请检查API配置";
                                 });
                             }
                         }
                         else if (value == null)
                         {
                             await Application.Current.Dispatcher.InvokeAsync(() =>
                             {
                                 StatusMessage = "未选择账户";
                             });
                         }
                     }
                     catch (Exception ex)
                     {
                         await Application.Current.Dispatcher.InvokeAsync(() =>
                         {
                             StatusMessage = $"切换账户失败: {ex.Message}";
                         });
                         
                         if (_logService != null)
                         {
                             await _logService.LogErrorAsync("切换账户时处理失败", ex, "账户管理");
                         }
                     }
                 });
            }
        }
    }

    [ObservableProperty]
    private string _statusMessage = "TestViewModel已加载 - 功能完善版本";

    [ObservableProperty]
    private bool _isLoading;

    // 市价下单属性
    [ObservableProperty]
    private string _marketSymbol = "";

    [ObservableProperty]
    private decimal _marketQuantity = 0;

    [ObservableProperty]
    private string _marketSide = "BUY";

    [ObservableProperty]
    private decimal _marketLeverage = 10;

    // 限价下单属性
    [ObservableProperty]
    private string _limitSymbol = "";

    [ObservableProperty]
    private decimal _limitQuantity = 0;

    [ObservableProperty]
    private decimal _limitPrice = 0;

    [ObservableProperty]
    private string _limitSide = "BUY";

    [ObservableProperty]
    private decimal _limitLeverage = 10;

    // 条件单属性
    [ObservableProperty]
    private decimal _longBreakoutPrice = 0;

    [ObservableProperty]
    private decimal _shortBreakdownPrice = 0;

    [ObservableProperty]
    private decimal _conditionalStopLossRatio = 10;

    // 其他属性
    [ObservableProperty]
    private decimal _latestPrice = 0;

    [ObservableProperty]
    private string _newSymbolInput = "";

    [ObservableProperty]
    private decimal _riskAmount = 0;

    [ObservableProperty]
    private OrderInfo? _selectedOpenOrder;

    // 精度信息显示属性
    [ObservableProperty]
    private string _precisionInfo = "";

    [ObservableProperty]
    private string _minPrecisionInfo = "";

    [ObservableProperty]
    private string _latestPriceInfo = "";

    [ObservableProperty]
    private string _selectedSymbolInfo = "";

    // 价格涨跌幅显示属性
    [ObservableProperty]
    private string _limitPriceChangePercent = "";

    [ObservableProperty]
    private string _longBreakoutPriceChangePercent = "";

    [ObservableProperty]
    private string _shortBreakdownPriceChangePercent = "";

    // 自动刷新属性
    private bool _autoRefreshEnabled = true;
    public bool AutoRefreshEnabled
    {
        get => _autoRefreshEnabled;
        set
        {
            if (SetProperty(ref _autoRefreshEnabled, value))
            {
                if (_autoRefreshEnabled)
                {
                    StartAutoRefresh();
                }
                else
                {
                    StopAutoRefresh();
                }
            }
        }
    }

    #endregion

    #region 集合

    public ObservableCollection<AccountInfo> Accounts { get; }
    public ObservableCollection<PositionInfo> Positions { get; }
    public ObservableCollection<OrderInfo> Orders { get; }
    public ObservableCollection<OrderInfo> OpenOrders { get; }
    public ObservableCollection<ConditionalOrder> ConditionalOrders { get; }
    public ObservableCollection<CandidateSymbol> CandidateSymbols { get; }
    public ObservableCollection<string> SymbolSuggestions { get; }

    #endregion

    #region 命令

    public RelayCommand PlaceMarketOrderCommand { get; private set; } = null!;
    public RelayCommand PlaceLimitOrderCommand { get; private set; } = null!;
    public RelayCommand AddLongConditionalOrderCommand { get; private set; } = null!;
    public RelayCommand AddShortConditionalOrderCommand { get; private set; } = null!;
    public RelayCommand AddSymbolFromInputCommand { get; private set; } = null!;
    public RelayCommand PlaceProfitOrderCommand { get; private set; } = null!;
    public RelayCommand ClosePositionCommand { get; private set; } = null!;
    public RelayCommand CloseAllPositionsCommand { get; private set; } = null!;
    public RelayCommand DecreaseRiskCapitalCommand { get; private set; } = null!;
    public RelayCommand IncreaseRiskCapitalCommand { get; private set; } = null!;
    
    // 账户管理命令
    public RelayCommand AddAccountCommand { get; private set; } = null!;
    public RelayCommand EditAccountCommand { get; private set; } = null!;
    public RelayCommand DeleteAccountCommand { get; private set; } = null!;
    
    // 日志和历史命令
    public RelayCommand OpenLogViewerCommand { get; private set; } = null!;
    public RelayCommand OpenTradeHistoryCommand { get; private set; } = null!;
    
    // 合约管理命令
    public RelayCommand RemoveCandidateSymbolCommand { get; private set; } = null!;
    
    // 数量和价格调整命令
    public RelayCommand<object> SetLeverageCommand { get; private set; } = null!;
    public RelayCommand<object> SetStopLossRatioCommand { get; private set; } = null!;
    public RelayCommand<object> AdjustRiskAmountCommand { get; private set; } = null!;
    
    // 价格调整命令
    public RelayCommand<object> AdjustLimitPriceCommand { get; private set; } = null!;
    public RelayCommand<object> AdjustLongBreakoutPriceCommand { get; private set; } = null!;
    public RelayCommand<object> AdjustShortBreakdownPriceCommand { get; private set; } = null!;

    #endregion

    #region 初始化

    private void InitializeCommands()
    {
        PlaceMarketOrderCommand = new RelayCommand(async () => await PlaceMarketOrderAsync());
        PlaceLimitOrderCommand = new RelayCommand(async () => await PlaceLimitOrderAsync());
        AddLongConditionalOrderCommand = new RelayCommand(async () => await AddLongConditionalOrderAsync());
        AddShortConditionalOrderCommand = new RelayCommand(async () => await AddShortConditionalOrderAsync());
        AddSymbolFromInputCommand = new RelayCommand(async () => await AddSymbolFromInputAsync());
        PlaceProfitOrderCommand = new RelayCommand(async () => await PlaceProfitOrderAsync());
        ClosePositionCommand = new RelayCommand(async () => await ClosePositionAsync());
        CloseAllPositionsCommand = new RelayCommand(async () => await CloseAllPositionsAsync());
        DecreaseRiskCapitalCommand = new RelayCommand(async () => await AdjustRiskCapitalAsync(false));
        IncreaseRiskCapitalCommand = new RelayCommand(async () => await AdjustRiskCapitalAsync(true));
        
        // 账户管理命令初始化
        AddAccountCommand = new RelayCommand(async () => await AddAccountAsync());
        EditAccountCommand = new RelayCommand(async () => await EditAccountAsync());
        DeleteAccountCommand = new RelayCommand(async () => await DeleteAccountAsync());
        
        // 日志和历史命令初始化
        OpenLogViewerCommand = new RelayCommand(async () => await OpenLogViewerAsync());
        OpenTradeHistoryCommand = new RelayCommand(async () => await OpenTradeHistoryAsync());
        
        // 合约管理命令初始化
        RemoveCandidateSymbolCommand = new RelayCommand(async () => await RemoveCandidateSymbolAsync());
        
        // 数量和价格调整命令初始化
        SetLeverageCommand = new RelayCommand<object>(param => SetLeverage(param));
        SetStopLossRatioCommand = new RelayCommand<object>(param => SetStopLossRatio(param));
        AdjustRiskAmountCommand = new RelayCommand<object>(async param => await AdjustRiskAmountAsync(param?.ToString() ?? ""));
        
        // 价格调整命令初始化
        AdjustLimitPriceCommand = new RelayCommand<object>(async param => await AdjustLimitPriceAsync(param?.ToString() ?? ""));
        AdjustLongBreakoutPriceCommand = new RelayCommand<object>(async param => await AdjustLongBreakoutPriceAsync(param?.ToString() ?? ""));
        AdjustShortBreakdownPriceCommand = new RelayCommand<object>(async param => await AdjustShortBreakdownPriceAsync(param?.ToString() ?? ""));
    }

    private void AddMockData()
    {
        // 添加模拟账户
        var mockAccount = new AccountInfo
        {
            Name = "测试账户",
            TotalWalletBalance = 10000m,
            TotalUnrealizedProfit = 150m,
            TotalMarginBalance = 10150m,
            TotalInitialMargin = 2000m,
            RiskCapitalTimes = 100m
        };
        
        Accounts.Add(mockAccount);
        SelectedAccount = mockAccount;

        // 添加模拟合约
        CandidateSymbols.Add(new CandidateSymbol { Symbol = "BTCUSDT" });
        CandidateSymbols.Add(new CandidateSymbol { Symbol = "ETHUSDT" });
        CandidateSymbols.Add(new CandidateSymbol { Symbol = "BNBUSDT" });

        StatusMessage = "模拟数据已加载 - 限价单、做多做空条件单功能可用";
    }

    #endregion

    #region 核心功能

    private async Task PlaceMarketOrderAsync()
    {
        if (string.IsNullOrEmpty(MarketSymbol) || MarketQuantity <= 0)
        {
            MessageBox.Show("请输入有效的合约和数量", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirmMessage = $"🚀 市价下单确认\n\n合约: {MarketSymbol}\n方向: {MarketSide}\n数量: {MarketQuantity}\n杠杆: {MarketLeverage}x\n\n确定执行市价下单吗？";
        var result = MessageBox.Show(confirmMessage, "市价下单确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            StatusMessage = "正在执行市价下单...";

            try
            {
                // 检查是否有选中的账户和API服务
                if (SelectedAccount == null)
                {
                    MessageBox.Show("请先选择账户", "下单失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StatusMessage = "❌ 下单失败: 未选择账户";
                    return;
                }

                if (_binanceService == null)
                {
                    MessageBox.Show("币安服务未初始化", "下单失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StatusMessage = "❌ 下单失败: 服务未初始化";
                    return;
                }

                // 构建下单请求
                var tradingRequest = new TradingRequest
                {
                    Symbol = MarketSymbol,
                    Side = MarketSide,
                    Type = "MARKET",
                    Quantity = MarketQuantity,
                    Price = 0, // 市价单不需要价格
                    ReduceOnly = false
                };

                // 调用币安API下单
                bool success = await _binanceService.PlaceOrderAsync(tradingRequest);

                if (success)
                {
                    // 市价单立即成交，不添加到委托列表
                    StatusMessage = $"✅ 市价下单成功 - {MarketSymbol} {MarketSide} {MarketQuantity}";
                    
                    MessageBox.Show($"✅ 市价下单执行成功！\n\n合约: {MarketSymbol}\n方向: {MarketSide}\n数量: {MarketQuantity}", 
                                  "交易成功", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 记录交易历史
                    if (_logService != null)
                    {
                        await _logService.LogInfoAsync($"市价下单成功: {MarketSymbol} {MarketSide} {MarketQuantity}", "交易");
                    }

                    // 自动添加到最近交易合约列表
                    await AddToRecentSymbolsAsync(MarketSymbol);
                }
                else
                {
                    StatusMessage = $"❌ 市价下单失败 - {MarketSymbol}";
                    MessageBox.Show("❌ 市价下单失败！请检查网络连接和API权限。", "下单失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ 市价下单异常: {ex.Message}";
                MessageBox.Show($"❌ 市价下单异常！\n\n错误信息: {ex.Message}", "下单异常", MessageBoxButton.OK, MessageBoxImage.Error);
                
                if (_logService != null)
                {
                    await _logService.LogErrorAsync("市价下单异常", ex, "交易");
                }
            }
        }
    }

    private async Task PlaceLimitOrderAsync()
    {
        if (string.IsNullOrEmpty(LimitSymbol) || LimitQuantity <= 0 || LimitPrice <= 0)
        {
            MessageBox.Show("请输入有效的合约、数量和价格", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirmMessage = $"🎯 限价下单确认\n\n合约: {LimitSymbol}\n方向: {LimitSide}\n数量: {LimitQuantity}\n价格: {LimitPrice}\n杠杆: {LimitLeverage}x\n\n确定执行限价下单吗？";
        var result = MessageBox.Show(confirmMessage, "限价下单确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            StatusMessage = "正在执行限价下单...";

            try
            {
                // 检查是否有选中的账户和API服务
                if (SelectedAccount == null)
                {
                    MessageBox.Show("请先选择账户", "下单失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StatusMessage = "❌ 下单失败: 未选择账户";
                    return;
                }

                if (_binanceService == null)
                {
                    MessageBox.Show("币安服务未初始化", "下单失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StatusMessage = "❌ 下单失败: 服务未初始化";
                    return;
                }

                // 构建限价下单请求
                var tradingRequest = new TradingRequest
                {
                    Symbol = LimitSymbol,
                    Side = LimitSide,
                    Type = "LIMIT",
                    Quantity = LimitQuantity,
                    Price = LimitPrice,
                    ReduceOnly = false
                };

                // 调用币安API下单
                bool success = await _binanceService.PlaceOrderAsync(tradingRequest);

                if (success)
                {
                    StatusMessage = $"✅ 限价下单成功 - {LimitSymbol} {LimitSide} {LimitQuantity}@{LimitPrice}";
                    
                    // 延迟刷新委托列表以获取真实的委托信息
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1000); // 等待1秒确保订单在交易所生效
                        await RefreshOpenOrdersAsync();
                    });
                    
                    MessageBox.Show($"✅ 限价下单执行成功！\n\n合约: {LimitSymbol}\n方向: {LimitSide}\n数量: {LimitQuantity}\n价格: {LimitPrice}", 
                                  "交易成功", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 记录交易历史
                    if (_logService != null)
                    {
                        await _logService.LogInfoAsync($"限价下单成功: {LimitSymbol} {LimitSide} {LimitQuantity}@{LimitPrice}", "交易");
                    }

                    // 自动添加到最近交易合约列表
                    await AddToRecentSymbolsAsync(LimitSymbol);
                }
                else
                {
                    StatusMessage = $"❌ 限价下单失败 - {LimitSymbol}";
                    MessageBox.Show("❌ 限价下单失败！请检查网络连接和API权限。", "下单失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ 限价下单异常: {ex.Message}";
                MessageBox.Show($"❌ 限价下单异常！\n\n错误信息: {ex.Message}", "下单异常", MessageBoxButton.OK, MessageBoxImage.Error);
                
                if (_logService != null)
                {
                    await _logService.LogErrorAsync("限价下单异常", ex, "交易");
                }
            }
        }
    }

    private async Task AddLongConditionalOrderAsync()
    {
        if (SelectedAccount == null)
        {
            MessageBox.Show("请先选择账户", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(MarketSymbol) || MarketQuantity <= 0 || LongBreakoutPrice <= 0)
        {
            MessageBox.Show("请输入有效的合约、数量和突破价格", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var currentPrice = LatestPrice > 0 ? LatestPrice : 46000m;
        if (LongBreakoutPrice <= currentPrice)
        {
            var result = MessageBox.Show(
                $"做多突破价格 {LongBreakoutPrice:F2} 低于或等于当前价格 {currentPrice:F2}\n" +
                "做多条件单通常应该设置在当前价格之上\n\n是否继续创建？",
                "价格设置提醒", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
                return;
        }

        var confirmMessage = $"🟢 做多条件单确认\n\n合约: {MarketSymbol}\n方向: 买入 (做多)\n数量: {MarketQuantity}\n突破价格: {LongBreakoutPrice:F4}\n当前价格: {currentPrice:F4}\n杠杆: {MarketLeverage}x\n\n条件: 当价格突破 {LongBreakoutPrice:F4} 时自动买入\n\n确定创建做多条件单吗？";
        var confirmResult = MessageBox.Show(confirmMessage, "做多条件单确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirmResult == MessageBoxResult.Yes)
        {
            StatusMessage = "正在创建做多条件单...";

            try
            {
                // 检查API服务
                if (_binanceService == null)
                {
                    MessageBox.Show("币安服务未初始化", "下单失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StatusMessage = "❌ 条件单创建失败: 服务未初始化";
                    return;
                }

                // 构建条件单请求 - 做多突破条件单
                var tradingRequest = new TradingRequest
                {
                    Symbol = MarketSymbol,
                    Side = "BUY",
                    Type = "STOP_MARKET",
                    Quantity = MarketQuantity,
                    Price = 0, // 条件单触发后以市价成交
                    StopPrice = LongBreakoutPrice, // 触发价格
                    ReduceOnly = false
                };

                // 调用币安API下条件单
                bool success = await _binanceService.PlaceOrderAsync(tradingRequest);

                if (success)
                {
                    // 下单成功，添加到本地条件单列表
                    var longConditionalOrder = new ConditionalOrder
                    {
                        Symbol = MarketSymbol,
                        Side = "BUY",
                        Type = "STOP_MARKET",
                        Quantity = MarketQuantity,
                        TriggerPrice = LongBreakoutPrice,
                        OrderPrice = 0,
                        Leverage = MarketLeverage,
                        Status = "PENDING",
                        CreateTime = DateTime.Now,
                        Remark = $"做多突破 - 当价格突破 {LongBreakoutPrice:F4} 时买入",
                        ReduceOnly = false
                    };

                    ConditionalOrders.Add(longConditionalOrder);
                    StatusMessage = $"✅ 做多条件单创建成功 - {MarketSymbol} 突破价 {LongBreakoutPrice:F4}";
                    
                    MessageBox.Show($"✅ 做多条件单创建成功！\n\n合约: {MarketSymbol}\n方向: 买入\n数量: {MarketQuantity}\n突破价: {LongBreakoutPrice:F4}\n\n条件单将在价格突破时自动执行", 
                                  "创建成功", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 记录交易历史
                    if (_logService != null)
                    {
                        await _logService.LogInfoAsync($"做多条件单创建成功: {MarketSymbol} 突破价 {LongBreakoutPrice:F4}", "交易");
                    }

                    // 自动添加到最近交易合约列表
                    await AddToRecentSymbolsAsync(MarketSymbol);
                }
                else
                {
                    StatusMessage = $"❌ 做多条件单创建失败 - {MarketSymbol}";
                    MessageBox.Show("❌ 做多条件单创建失败！请检查网络连接和API权限。", "创建失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ 做多条件单异常: {ex.Message}";
                MessageBox.Show($"❌ 做多条件单创建异常！\n\n错误信息: {ex.Message}", "创建异常", MessageBoxButton.OK, MessageBoxImage.Error);
                
                if (_logService != null)
                {
                    await _logService.LogErrorAsync("做多条件单异常", ex, "交易");
                }
            }
        }
    }

    private async Task AddShortConditionalOrderAsync()
    {
        if (SelectedAccount == null)
        {
            MessageBox.Show("请先选择账户", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(MarketSymbol) || MarketQuantity <= 0 || ShortBreakdownPrice <= 0)
        {
            MessageBox.Show("请输入有效的合约、数量和跌破价格", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var currentPrice = LatestPrice > 0 ? LatestPrice : 46000m;
        if (ShortBreakdownPrice >= currentPrice)
        {
            var result = MessageBox.Show(
                $"做空跌破价格 {ShortBreakdownPrice:F2} 高于或等于当前价格 {currentPrice:F2}\n" +
                "做空条件单通常应该设置在当前价格之下\n\n是否继续创建？",
                "价格设置提醒", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
                return;
        }

        var confirmMessage = $"🔴 做空条件单确认\n\n合约: {MarketSymbol}\n方向: 卖出 (做空)\n数量: {MarketQuantity}\n跌破价格: {ShortBreakdownPrice:F4}\n当前价格: {currentPrice:F4}\n杠杆: {MarketLeverage}x\n\n条件: 当价格跌破 {ShortBreakdownPrice:F4} 时自动卖出\n\n确定创建做空条件单吗？";
        var confirmResult = MessageBox.Show(confirmMessage, "做空条件单确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirmResult == MessageBoxResult.Yes)
        {
            StatusMessage = "正在创建做空条件单...";

            try
            {
                // 检查API服务
                if (_binanceService == null)
                {
                    MessageBox.Show("币安服务未初始化", "下单失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StatusMessage = "❌ 条件单创建失败: 服务未初始化";
                    return;
                }

                // 构建条件单请求 - 做空跌破条件单
                var tradingRequest = new TradingRequest
                {
                    Symbol = MarketSymbol,
                    Side = "SELL",
                    Type = "STOP_MARKET",
                    Quantity = MarketQuantity,
                    Price = 0, // 条件单触发后以市价成交
                    StopPrice = ShortBreakdownPrice, // 触发价格
                    ReduceOnly = false
                };

                // 调用币安API下条件单
                bool success = await _binanceService.PlaceOrderAsync(tradingRequest);

                if (success)
                {
                    // 下单成功，添加到本地条件单列表
                    var shortConditionalOrder = new ConditionalOrder
                    {
                        Symbol = MarketSymbol,
                        Side = "SELL",
                        Type = "STOP_MARKET",
                        Quantity = MarketQuantity,
                        TriggerPrice = ShortBreakdownPrice,
                        OrderPrice = 0,
                        Leverage = MarketLeverage,
                        Status = "PENDING",
                        CreateTime = DateTime.Now,
                        Remark = $"做空跌破 - 当价格跌破 {ShortBreakdownPrice:F4} 时卖出",
                        ReduceOnly = false
                    };

                    ConditionalOrders.Add(shortConditionalOrder);
                    StatusMessage = $"✅ 做空条件单创建成功 - {MarketSymbol} 跌破价 {ShortBreakdownPrice:F4}";
                    
                    MessageBox.Show($"✅ 做空条件单创建成功！\n\n合约: {MarketSymbol}\n方向: 卖出\n数量: {MarketQuantity}\n跌破价: {ShortBreakdownPrice:F4}\n\n条件单将在价格跌破时自动执行", 
                                  "创建成功", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 记录交易历史
                    if (_logService != null)
                    {
                        await _logService.LogInfoAsync($"做空条件单创建成功: {MarketSymbol} 跌破价 {ShortBreakdownPrice:F4}", "交易");
                    }

                    // 自动添加到最近交易合约列表
                    await AddToRecentSymbolsAsync(MarketSymbol);
                }
                else
                {
                    StatusMessage = $"❌ 做空条件单创建失败 - {MarketSymbol}";
                    MessageBox.Show("❌ 做空条件单创建失败！请检查网络连接和API权限。", "创建失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ 做空条件单异常: {ex.Message}";
                MessageBox.Show($"❌ 做空条件单创建异常！\n\n错误信息: {ex.Message}", "创建异常", MessageBoxButton.OK, MessageBoxImage.Error);
                
                if (_logService != null)
                {
                    await _logService.LogErrorAsync("做空条件单异常", ex, "交易");
                }
            }
        }
    }

    private async Task AddSymbolFromInputAsync()
    {
        if (!string.IsNullOrEmpty(NewSymbolInput))
        {
            CandidateSymbols.Add(new CandidateSymbol { Symbol = NewSymbolInput.ToUpper() });
            NewSymbolInput = "";
            StatusMessage = $"合约 {NewSymbolInput} 已添加";
        }
        await Task.CompletedTask;
    }

    // 自动添加最近交易的合约到候选列表
    private async Task AddToRecentSymbolsAsync(string symbol)
    {
        if (string.IsNullOrEmpty(symbol)) return;

        try
        {
            var upperSymbol = symbol.ToUpper();
            
            // 检查是否已存在
            var existingSymbol = CandidateSymbols.FirstOrDefault(s => s.Symbol.Equals(upperSymbol, StringComparison.OrdinalIgnoreCase));
            
            if (existingSymbol == null)
            {
                // 添加到候选列表
                CandidateSymbols.Add(new CandidateSymbol { Symbol = upperSymbol });
                
                // 保存到配置文件
                if (_configService != null)
                {
                    await _configService.SaveCandidateSymbolsAsync(CandidateSymbols.ToList());
                }
                
                Console.WriteLine($"自动添加交易合约到候选列表: {upperSymbol}");
            }
            else
            {
                // 如果已存在，移到最后（最近使用）
                CandidateSymbols.Remove(existingSymbol);
                CandidateSymbols.Add(existingSymbol);
                
                // 保存到配置文件
                if (_configService != null)
                {
                    await _configService.SaveCandidateSymbolsAsync(CandidateSymbols.ToList());
                }
                
                Console.WriteLine($"更新交易合约位置到最新: {upperSymbol}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"添加最近交易合约失败: {ex.Message}");
        }
    }

    private async Task PlaceProfitOrderAsync()
    {
        try
        {
            // 检查账户
            if (SelectedAccount == null)
            {
                MessageBox.Show("请先选择账户", "保本加仓失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 检查是否有持仓
            var currentPosition = Positions.FirstOrDefault(p => p.Symbol == MarketSymbol && Math.Abs(p.PositionAmt) > 0);
            if (currentPosition == null)
            {
                MessageBox.Show("没有找到当前合约的持仓", "保本加仓失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 检查浮盈条件 - 浮盈必须大于等于风险金的50%
            var baseRiskAmount = GetBaseRiskAmount();
            var requiredProfit = baseRiskAmount * 0.5m;
            
            if (currentPosition.UnRealizedProfit < requiredProfit)
            {
                MessageBox.Show($"💰 保本加仓条件不满足\n\n当前浮盈: {currentPosition.UnRealizedProfit:F2}\n要求浮盈: {requiredProfit:F2} (风险金50%)\n风险金: {baseRiskAmount:F2}", 
                              "保本加仓失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 确认对话框
            var confirmMessage = $"💰 保本加仓确认\n\n合约: {MarketSymbol}\n当前持仓: {currentPosition.PositionAmt}\n当前浮盈: {currentPosition.UnRealizedProfit:F2}\n加仓数量: {MarketQuantity}\n\n执行后将下止损单保护本金\n\n确定执行保本加仓吗？";
            var result = MessageBox.Show(confirmMessage, "保本加仓确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                StatusMessage = "正在执行保本加仓...";

                if (_binanceService == null)
                {
                    MessageBox.Show("币安服务未初始化", "保本加仓失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 1. 先下市价加仓单
                var addPositionRequest = new TradingRequest
                {
                    Symbol = MarketSymbol,
                    Side = currentPosition.PositionAmt > 0 ? "BUY" : "SELL", // 与当前持仓方向一致
                    Type = "MARKET",
                    Quantity = MarketQuantity,
                    Price = 0,
                    ReduceOnly = false
                };

                bool addPositionSuccess = await _binanceService.PlaceOrderAsync(addPositionRequest);

                if (addPositionSuccess)
                {
                    // 2. 下止损单保护本金
                    var stopLossPrice = currentPosition.EntryPrice; // 以入场价作为止损价
                    var totalQuantity = Math.Abs(currentPosition.PositionAmt) + MarketQuantity;

                    var stopLossRequest = new TradingRequest
                    {
                        Symbol = MarketSymbol,
                        Side = currentPosition.PositionAmt > 0 ? "SELL" : "BUY", // 与持仓方向相反
                        Type = "STOP_MARKET",
                        Quantity = totalQuantity,
                        Price = 0,
                        StopPrice = stopLossPrice,
                        ReduceOnly = true // 止损单必须是reduceOnly
                    };

                    bool stopLossSuccess = await _binanceService.PlaceOrderAsync(stopLossRequest);

                    if (stopLossSuccess)
                    {
                        StatusMessage = $"✅ 保本加仓成功 - 已加仓 {MarketQuantity}，止损价 {stopLossPrice:F4}";
                        MessageBox.Show($"✅ 保本加仓执行成功！\n\n加仓数量: {MarketQuantity}\n止损价格: {stopLossPrice:F4}\n止损数量: {totalQuantity}", 
                                      "保本加仓成功", MessageBoxButton.OK, MessageBoxImage.Information);

                        // 记录交易历史
                        if (_logService != null)
                        {
                            await _logService.LogInfoAsync($"保本加仓成功: {MarketSymbol} 加仓{MarketQuantity} 止损{stopLossPrice:F4}", "交易");
                        }

                        // 自动添加到最近交易合约列表
                        await AddToRecentSymbolsAsync(MarketSymbol);
                    }
                    else
                    {
                        StatusMessage = "⚠️ 加仓成功但止损单下单失败，请手动设置止损";
                        MessageBox.Show("⚠️ 加仓成功但止损单下单失败\n\n请手动设置止损单保护本金！", "部分成功", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    StatusMessage = "❌ 保本加仓失败";
                    MessageBox.Show("❌ 加仓下单失败！请检查网络连接和API权限。", "保本加仓失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 保本加仓异常: {ex.Message}";
            MessageBox.Show($"❌ 保本加仓异常！\n\n错误信息: {ex.Message}", "保本加仓异常", MessageBoxButton.OK, MessageBoxImage.Error);
            
            if (_logService != null)
            {
                await _logService.LogErrorAsync("保本加仓异常", ex, "交易");
            }
        }
    }

    private async Task ClosePositionAsync()
    {
        try
        {
            // 检查账户
            if (SelectedAccount == null)
            {
                MessageBox.Show("请先选择账户", "平仓失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 检查是否有持仓
            var currentPosition = Positions.FirstOrDefault(p => p.Symbol == MarketSymbol && Math.Abs(p.PositionAmt) > 0);
            if (currentPosition == null)
            {
                MessageBox.Show("没有找到当前合约的持仓", "平仓失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 确认对话框
            var confirmMessage = $"🔴 平仓确认\n\n合约: {MarketSymbol}\n持仓数量: {currentPosition.PositionAmt}\n持仓方向: {(currentPosition.PositionAmt > 0 ? "多头" : "空头")}\n浮动盈亏: {currentPosition.UnRealizedProfit:F2}\n\n确定执行平仓吗？";
            var result = MessageBox.Show(confirmMessage, "平仓确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                StatusMessage = "正在执行平仓...";

                if (_binanceService == null)
                {
                    MessageBox.Show("币安服务未初始化", "平仓失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 构建平仓请求
                var closeRequest = new TradingRequest
                {
                    Symbol = MarketSymbol,
                    Side = currentPosition.PositionAmt > 0 ? "SELL" : "BUY", // 与持仓方向相反
                    Type = "MARKET",
                    Quantity = Math.Abs(currentPosition.PositionAmt),
                    Price = 0,
                    ReduceOnly = true // 平仓单必须是reduceOnly
                };

                bool success = await _binanceService.PlaceOrderAsync(closeRequest);

                if (success)
                {
                    StatusMessage = $"✅ 平仓成功 - {MarketSymbol} {Math.Abs(currentPosition.PositionAmt)}";
                    MessageBox.Show($"✅ 平仓执行成功！\n\n合约: {MarketSymbol}\n平仓数量: {Math.Abs(currentPosition.PositionAmt)}\n盈亏: {currentPosition.UnRealizedProfit:F2}", 
                                  "平仓成功", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 记录交易历史
                    if (_logService != null)
                    {
                        await _logService.LogInfoAsync($"平仓成功: {MarketSymbol} {Math.Abs(currentPosition.PositionAmt)} 盈亏{currentPosition.UnRealizedProfit:F2}", "交易");
                    }

                    // 自动添加到最近交易合约列表
                    await AddToRecentSymbolsAsync(MarketSymbol);
                }
                else
                {
                    StatusMessage = $"❌ 平仓失败 - {MarketSymbol}";
                    MessageBox.Show("❌ 平仓下单失败！请检查网络连接和API权限。", "平仓失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 平仓异常: {ex.Message}";
            MessageBox.Show($"❌ 平仓异常！\n\n错误信息: {ex.Message}", "平仓异常", MessageBoxButton.OK, MessageBoxImage.Error);
            
            if (_logService != null)
            {
                await _logService.LogErrorAsync("平仓异常", ex, "交易");
            }
        }
    }

    private async Task CloseAllPositionsAsync()
    {
        try
        {
            // 检查账户
            if (SelectedAccount == null)
            {
                MessageBox.Show("请先选择账户", "一键全平失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_binanceService == null)
            {
                MessageBox.Show("币安服务未初始化", "一键全平失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 获取所有有持仓的合约
            var activePositions = Positions.Where(p => Math.Abs(p.PositionAmt) > 0).ToList();
            
            if (!activePositions.Any())
            {
                MessageBox.Show("没有找到任何持仓", "一键全平", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 计算总盈亏
            var totalPnl = activePositions.Sum(p => p.UnRealizedProfit);
            var positionSummary = string.Join("\n", activePositions.Select(p => 
                $"{p.Symbol}: {p.PositionAmt} (盈亏: {p.UnRealizedProfit:F2})"));

            var confirmMessage = $"⚡ 确认一键全平？\n\n将平掉以下所有持仓:\n{positionSummary}\n\n总盈亏: {totalPnl:F2}\n\n确定执行一键全平吗？";
            var result = MessageBox.Show(confirmMessage, "一键全平确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                StatusMessage = "正在执行一键全平...";
                int successCount = 0;
                int failCount = 0;
                var failedSymbols = new List<string>();

                foreach (var position in activePositions)
                {
                    try
                    {
                        // 构建平仓请求
                        var closeRequest = new TradingRequest
                        {
                            Symbol = position.Symbol,
                            Side = position.PositionAmt > 0 ? "SELL" : "BUY", // 与持仓方向相反
                            Type = "MARKET",
                            Quantity = Math.Abs(position.PositionAmt),
                            Price = 0,
                            ReduceOnly = true // 平仓单必须是reduceOnly
                        };

                        bool success = await _binanceService.PlaceOrderAsync(closeRequest);

                        if (success)
                        {
                            successCount++;
                            StatusMessage = $"正在执行一键全平... ({successCount}/{activePositions.Count})";
                            
                            // 自动添加成功平仓的合约到最近交易列表
                            await AddToRecentSymbolsAsync(position.Symbol);
                        }
                        else
                        {
                            failCount++;
                            failedSymbols.Add(position.Symbol);
                        }

                        // 添加小延迟避免API限制
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        failedSymbols.Add($"{position.Symbol}({ex.Message})");
                    }
                }

                // 显示结果
                if (failCount == 0)
                {
                    StatusMessage = $"✅ 一键全平成功 - 已平仓 {successCount} 个合约";
                    MessageBox.Show($"✅ 一键全平执行成功！\n\n成功平仓: {successCount} 个合约\n总盈亏: {totalPnl:F2}", 
                                  "一键全平成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = $"⚠️ 一键全平部分成功 - 成功{successCount} 失败{failCount}";
                    var failedInfo = string.Join(", ", failedSymbols);
                    MessageBox.Show($"⚠️ 一键全平部分成功！\n\n成功平仓: {successCount} 个合约\n失败平仓: {failCount} 个合约\n失败列表: {failedInfo}", 
                                  "一键全平部分成功", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // 记录交易历史
                if (_logService != null)
                {
                    await _logService.LogInfoAsync($"一键全平: 成功{successCount} 失败{failCount} 总盈亏{totalPnl:F2}", "交易");
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 一键全平异常: {ex.Message}";
            MessageBox.Show($"❌ 一键全平异常！\n\n错误信息: {ex.Message}", "一键全平异常", MessageBoxButton.OK, MessageBoxImage.Error);
            
            if (_logService != null)
            {
                await _logService.LogErrorAsync("一键全平异常", ex, "交易");
            }
        }
    }

    private async Task AdjustRiskCapitalAsync(bool increase)
    {
        if (SelectedAccount != null)
        {
            if (increase)
            {
                SelectedAccount.RiskCapitalTimes += 1;
                StatusMessage = $"风险次数已增加到 {SelectedAccount.RiskCapitalTimes}";
            }
            else if (SelectedAccount.RiskCapitalTimes > 1)
            {
                SelectedAccount.RiskCapitalTimes -= 1;
                StatusMessage = $"风险次数已减少到 {SelectedAccount.RiskCapitalTimes}";
            }
        }
        await Task.CompletedTask;
    }

    // 账户管理功能
    private async Task AddAccountAsync()
    {
        try
        {
            var accountWindow = new OrderWatch.Views.AccountConfigWindow();
            accountWindow.Owner = Application.Current.MainWindow;
            
            if (accountWindow.ShowDialog() == true)
            {
                // 添加到账户列表
                Accounts.Add(accountWindow.AccountInfo);
                
                // 如果是第一个账户，自动选择
                if (SelectedAccount == null)
                {
                    SelectedAccount = accountWindow.AccountInfo;
                }
                
                // 保存账户配置（如果有ConfigService）
                if (_configService != null)
                {
                    await _configService.SaveAccountAsync(accountWindow.AccountInfo);
                }
                
                StatusMessage = $"✅ 账户添加成功: {accountWindow.AccountInfo.Name}";
                
                if (_logService != null)
                {
                    await _logService.LogInfoAsync($"添加账户: {accountWindow.AccountInfo.Name}", "账户管理");
                }
                
                MessageBox.Show($"账户 '{accountWindow.AccountInfo.Name}' 添加成功！", "添加成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"添加账户失败: {ex.Message}";
            
            if (_logService != null)
            {
                await _logService.LogErrorAsync("添加账户失败", ex, "账户管理");
            }
            
            MessageBox.Show($"添加账户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task EditAccountAsync()
    {
        if (SelectedAccount == null)
        {
            MessageBox.Show("请先选择要编辑的账户", "编辑账户", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            // 创建账户的副本用于编辑
            var accountCopy = new AccountInfo
            {
                Name = SelectedAccount.Name,
                ApiKey = SelectedAccount.ApiKey,
                SecretKey = SelectedAccount.SecretKey,
                IsTestNet = SelectedAccount.IsTestNet,
                RiskCapitalTimes = SelectedAccount.RiskCapitalTimes,
                TotalWalletBalance = SelectedAccount.TotalWalletBalance,
                TotalUnrealizedProfit = SelectedAccount.TotalUnrealizedProfit,
                TotalMarginBalance = SelectedAccount.TotalMarginBalance,
                TotalInitialMargin = SelectedAccount.TotalInitialMargin
            };
            
            var accountWindow = new OrderWatch.Views.AccountConfigWindow(accountCopy);
            accountWindow.Owner = Application.Current.MainWindow;
            
            if (accountWindow.ShowDialog() == true)
            {
                // 更新原账户信息
                var originalIndex = Accounts.IndexOf(SelectedAccount);
                if (originalIndex >= 0)
                {
                    // 保留余额等交易数据，只更新配置信息
                    SelectedAccount.Name = accountWindow.AccountInfo.Name;
                    SelectedAccount.ApiKey = accountWindow.AccountInfo.ApiKey;
                    SelectedAccount.SecretKey = accountWindow.AccountInfo.SecretKey;
                    SelectedAccount.IsTestNet = accountWindow.AccountInfo.IsTestNet;
                    SelectedAccount.RiskCapitalTimes = accountWindow.AccountInfo.RiskCapitalTimes;
                    
                    // 保存账户配置（如果有ConfigService）
                    if (_configService != null)
                    {
                        await _configService.SaveAccountAsync(SelectedAccount);
                    }
                    
                    StatusMessage = $"✅ 账户更新成功: {SelectedAccount.Name}";
                    
                    if (_logService != null)
                    {
                        await _logService.LogInfoAsync($"编辑账户: {SelectedAccount.Name}", "账户管理");
                    }
                    
                    MessageBox.Show($"账户 '{SelectedAccount.Name}' 更新成功！", "更新成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"编辑账户失败: {ex.Message}";
            
            if (_logService != null)
            {
                await _logService.LogErrorAsync("编辑账户失败", ex, "账户管理");
            }
            
            MessageBox.Show($"编辑账户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeleteAccountAsync()
    {
        if (SelectedAccount == null)
        {
            MessageBox.Show("请先选择要删除的账户", "删除账户", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            var result = MessageBox.Show(
                $"确定要删除账户 '{SelectedAccount.Name}' 吗？\n\n删除后将无法恢复！", 
                "确认删除", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                var accountName = SelectedAccount.Name;
                
                // 从配置中删除（如果有ConfigService）
                if (_configService != null)
                {
                    await _configService.DeleteAccountAsync(accountName);
                }
                
                // 从列表中删除
                Accounts.Remove(SelectedAccount);
                
                // 如果删除的是当前选中账户，清空选择
                SelectedAccount = null;
                
                // 如果还有其他账户，选择第一个
                if (Accounts.Count > 0)
                {
                    SelectedAccount = Accounts[0];
                }
                
                StatusMessage = $"✅ 账户删除成功: {accountName}";
                
                if (_logService != null)
                {
                    await _logService.LogInfoAsync($"删除账户: {accountName}", "账户管理");
                }
                
                MessageBox.Show($"账户 '{accountName}' 已删除", "删除成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除账户失败: {ex.Message}";
            
            if (_logService != null)
            {
                await _logService.LogErrorAsync("删除账户失败", ex, "账户管理");
            }
            
            MessageBox.Show($"删除账户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 日志和历史功能
    private async Task OpenLogViewerAsync()
    {
        try
        {
            var logViewerWindow = new OrderWatch.Views.LogViewerWindow();
            logViewerWindow.Show();
            StatusMessage = "日志查看器已打开";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开日志查看器失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        await Task.CompletedTask;
    }

    private async Task OpenTradeHistoryAsync()
    {
        try
        {
            var tradeHistoryWindow = new OrderWatch.Views.TradeHistoryWindow();
            tradeHistoryWindow.Show();
            StatusMessage = "交易历史窗口已打开";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开交易历史失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        await Task.CompletedTask;
    }

    // 合约管理功能
    private async Task RemoveCandidateSymbolAsync()
    {
        if (CandidateSymbols.Count > 0)
        {
            var lastSymbol = CandidateSymbols.Last();
            CandidateSymbols.Remove(lastSymbol);
            StatusMessage = $"已删除合约: {lastSymbol.Symbol}";
        }
        await Task.CompletedTask;
    }

    // 数量和价格调整功能
    private void SetLeverage(object? parameter)
    {
        if (parameter != null && decimal.TryParse(parameter.ToString(), out var leverage))
        {
            MarketLeverage = leverage;
            LimitLeverage = leverage;
            StatusMessage = $"杠杆已设置为: {leverage}x";
        }
    }

    private void SetStopLossRatio(object? parameter)
    {
        if (parameter != null && decimal.TryParse(parameter.ToString(), out var ratio))
        {
            ConditionalStopLossRatio = ratio;
            StatusMessage = $"止损比例已设置为: {ratio}%";
        }
    }

    private async Task AdjustRiskAmountAsync(string action)
    {
        var baseAmount = GetBaseRiskAmount();
        
        switch (action)
        {
            case "double":
                RiskAmount = Math.Round(RiskAmount + baseAmount, 0);
                StatusMessage = $"风险金额已加倍至: {RiskAmount:F0}";
                break;
            case "half":
                RiskAmount = Math.Round(RiskAmount + (baseAmount / 2), 0);
                StatusMessage = $"风险金额已加半至: {RiskAmount:F0}";
                break;
            case "reduce_half":
                RiskAmount = Math.Round(Math.Max(0, RiskAmount - (baseAmount / 2)), 0);
                StatusMessage = $"风险金额已减半至: {RiskAmount:F0}";
                break;
        }
        
        // 重新计算数量，使用当前合约的精度
        await RecalculateQuantityWithPrecisionAsync();
    }

    private async Task RecalculateQuantityWithPrecisionAsync()
    {
        try
        {
            if (LatestPrice <= 0 || RiskAmount <= 0)
                return;

            // 获取当前选中合约的精度信息
            int quantityPrecision = 4; // 默认精度
            decimal minQty = 0.001m;

            if (!string.IsNullOrEmpty(MarketSymbol) && _binanceSymbolService != null)
            {
                try
                {
                    var precision = await _binanceSymbolService.GetSymbolPrecisionAsync(MarketSymbol);
                    quantityPrecision = precision.quantityPrecision;
                    minQty = precision.minQty;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"获取精度信息失败: {ex.Message}");
                }
            }

            // 计算新的数量 - 正确算法：市值 = 风险金额 / 止损比例，数量 = 市值 / 最新价
            var stopLossRatio = ConditionalStopLossRatio / 100m; // 转换为小数
            var marketValue = RiskAmount / stopLossRatio; // 市值 = A / 止损比例
            var calculatedQuantity = marketValue / LatestPrice; // 数量 = 市值 / 最新价

            // 使用合约服务调整数量到有效值
            if (_binanceSymbolService != null)
            {
                try
                {
                    calculatedQuantity = await _binanceSymbolService.AdjustQuantityToValidAsync(MarketSymbol, calculatedQuantity);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"调整数量失败: {ex.Message}");
                    // 降级到简单的四舍五入
                    calculatedQuantity = Math.Round(calculatedQuantity, quantityPrecision);
                }
            }
            else
            {
                calculatedQuantity = Math.Round(calculatedQuantity, quantityPrecision);
            }

            // 确保不小于最小数量
            if (calculatedQuantity < minQty)
            {
                calculatedQuantity = minQty;
            }

            // 更新数量
            MarketQuantity = calculatedQuantity;
            LimitQuantity = calculatedQuantity;

            Console.WriteLine($"重新计算数量: {calculatedQuantity.ToString($"F{quantityPrecision}")} (精度: {quantityPrecision}位, 最小: {minQty})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"重新计算数量失败: {ex.Message}");
        }
    }

    private decimal GetBaseRiskAmount()
    {
        if (SelectedAccount != null && SelectedAccount.RiskCapitalTimes > 0)
        {
            return SelectedAccount.TotalWalletBalance / SelectedAccount.RiskCapitalTimes;
        }
        return 100m; // 默认风险金额
    }

    // 价格调整方法
    private async Task AdjustLimitPriceAsync(string factorString)
    {
        if (decimal.TryParse(factorString, out decimal factor))
        {
            LimitPrice = LimitPrice * factor;
            UpdateLimitPriceChangePercent();
            await Task.CompletedTask;
        }
    }

    private async Task AdjustLongBreakoutPriceAsync(string factorString)
    {
        if (decimal.TryParse(factorString, out decimal factor))
        {
            LongBreakoutPrice = LongBreakoutPrice * factor;
            UpdateLongBreakoutPriceChangePercent();
            await Task.CompletedTask;
        }
    }

    private async Task AdjustShortBreakdownPriceAsync(string factorString)
    {
        if (decimal.TryParse(factorString, out decimal factor))
        {
            ShortBreakdownPrice = ShortBreakdownPrice * factor;
            UpdateShortBreakdownPriceChangePercent();
            await Task.CompletedTask;
        }
    }

    // 更新价格涨跌幅显示
    private void UpdateLimitPriceChangePercent()
    {
        if (LatestPrice > 0 && LimitPrice > 0)
        {
            var changePercent = ((LimitPrice - LatestPrice) / LatestPrice) * 100;
            LimitPriceChangePercent = changePercent > 0 ? $"+{changePercent:F1}%" : $"{changePercent:F1}%";
        }
        else
        {
            LimitPriceChangePercent = "";
        }
    }

    private void UpdateLongBreakoutPriceChangePercent()
    {
        if (LatestPrice > 0 && LongBreakoutPrice > 0)
        {
            var changePercent = ((LongBreakoutPrice - LatestPrice) / LatestPrice) * 100;
            LongBreakoutPriceChangePercent = changePercent > 0 ? $"+{changePercent:F1}%" : $"{changePercent:F1}%";
        }
        else
        {
            LongBreakoutPriceChangePercent = "";
        }
    }

    private void UpdateShortBreakdownPriceChangePercent()
    {
        if (LatestPrice > 0 && ShortBreakdownPrice > 0)
        {
            var changePercent = ((ShortBreakdownPrice - LatestPrice) / LatestPrice) * 100;
            ShortBreakdownPriceChangePercent = changePercent > 0 ? $"+{changePercent:F1}%" : $"{changePercent:F1}%";
        }
        else
        {
            ShortBreakdownPriceChangePercent = "";
        }
    }

    // 双击合约自动填充功能
    public async Task AutoFillSymbolToOrderAreasAsync(string symbol)
    {
        try
        {
            if (SelectedAccount == null)
            {
                MessageBox.Show("请先选择账户", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StatusMessage = $"正在获取 {symbol} 的交易信息...";

            // 自动填充合约名称到所有下单区域
            MarketSymbol = symbol;
            LimitSymbol = symbol;

            // 获取合约详细信息和最新价格
            decimal latestPrice = 0;
            int pricePrecision = 2;
            int quantityPrecision = 4;
            decimal minQty = 0.001m;

            if (_binanceSymbolService != null)
            {
                try
                {
                                         // 获取最新价格
                     if (_binanceService != null)
                     {
                         latestPrice = await _binanceService.GetLatestPriceAsync(symbol);
                     }

                    // 获取合约精度信息
                    var precision = await _binanceSymbolService.GetSymbolPrecisionAsync(symbol);
                    pricePrecision = precision.pricePrecision;
                    quantityPrecision = precision.quantityPrecision;
                    minQty = precision.minQty;

                    Console.WriteLine($"获取到 {symbol} 信息 - 价格:{latestPrice} 价格精度:{pricePrecision} 数量精度:{quantityPrecision} 最小数量:{minQty}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"获取合约信息失败: {ex.Message}");
                }
            }

            // 如果API获取失败，使用默认价格
            if (latestPrice <= 0)
            {
                latestPrice = symbol.Contains("BTC") ? 46000m : symbol.Contains("ETH") ? 2500m : 300m;
                Console.WriteLine($"使用默认价格: {latestPrice}");
            }

            // 设置价格信息
            LatestPrice = latestPrice;
            LimitPrice = latestPrice;
            LongBreakoutPrice = Math.Round(latestPrice * 1.02m, pricePrecision); // 突破价设为当前价+2%
            ShortBreakdownPrice = Math.Round(latestPrice * 0.98m, pricePrecision); // 跌破价设为当前价-2%
            
            // 更新价格涨跌幅显示
            UpdateLimitPriceChangePercent();
            UpdateLongBreakoutPriceChangePercent();
            UpdateShortBreakdownPriceChangePercent();

            // 更新精度信息显示
            LatestPriceInfo = $"{latestPrice.ToString($"F{pricePrecision}")}";
            PrecisionInfo = $"价格精度: {pricePrecision}位";
            MinPrecisionInfo = $"数量精度: {quantityPrecision}位, 最小: {minQty}";
            SelectedSymbolInfo = $"{symbol} | 价格: {latestPrice.ToString($"F{pricePrecision}")} | 数量精度: {quantityPrecision}位";

            // 计算以损定量（风险金）- 转为整数
            var baseRiskAmount = GetBaseRiskAmount();
            RiskAmount = Math.Round(baseRiskAmount, 0); // 四舍五入到整数
            
            // 根据风险金和价格计算数量，确保符合精度要求
            if (latestPrice > 0)
            {
                // 正确算法：市值 = 风险金额 / 止损比例，数量 = 市值 / 最新价
                var stopLossRatio = ConditionalStopLossRatio / 100m; // 转换为小数
                var marketValue = baseRiskAmount / stopLossRatio; // 市值 = A / 止损比例
                var calculatedQuantity = marketValue / latestPrice; // 数量 = 市值 / 最新价
                
                // 使用合约服务调整数量到有效值
                if (_binanceSymbolService != null)
                {
                    try
                    {
                        calculatedQuantity = await _binanceSymbolService.AdjustQuantityToValidAsync(symbol, calculatedQuantity);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"调整数量失败: {ex.Message}");
                        // 降级到简单的四舍五入
                        calculatedQuantity = Math.Round(calculatedQuantity, quantityPrecision);
                    }
                }
                else
                {
                    calculatedQuantity = Math.Round(calculatedQuantity, quantityPrecision);
                }

                // 确保不小于最小数量
                if (calculatedQuantity < minQty)
                {
                    calculatedQuantity = minQty;
                }

                MarketQuantity = calculatedQuantity;
                LimitQuantity = calculatedQuantity;
            }

            var displayMarketValue = baseRiskAmount / (ConditionalStopLossRatio / 100m);
            StatusMessage = $"✅ 已自动填充 {symbol} - 价格:{latestPrice.ToString($"F{pricePrecision}")} 数量:{MarketQuantity.ToString($"F{quantityPrecision}")} 风险金:{baseRiskAmount:F2} 市值:{displayMarketValue:F2} 止损:{ConditionalStopLossRatio}%";
        }
        catch (Exception ex)
        {
            StatusMessage = $"自动填充失败: {ex.Message}";
            Console.WriteLine($"AutoFillSymbolToOrderAreasAsync 异常: {ex}");
            
            if (_logService != null)
            {
                await _logService.LogErrorAsync("自动填充合约信息失败", ex, "自动填充");
            }
        }
    }

    public async Task AutoFillPositionToOrderAreasAsync(PositionInfo position)
    {
        try
        {
            // 根据持仓信息自动填充
            MarketSymbol = position.Symbol;
            LimitSymbol = position.Symbol;
            LimitPrice = position.MarkPrice;
            LatestPrice = position.MarkPrice;

            // 设置为相反方向（平仓）
            MarketSide = position.PositionSide == "LONG" ? "SELL" : "BUY";
            LimitSide = MarketSide;

            // 数量设为持仓数量
            MarketQuantity = Math.Abs(position.PositionAmt);
            LimitQuantity = MarketQuantity;

            StatusMessage = $"✅ 已自动填充持仓信息 - {position.Symbol} 平仓方向:{MarketSide} 数量:{MarketQuantity:F4}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"自动填充持仓失败: {ex.Message}";
        }
        await Task.CompletedTask;
    }

    // 合约自动完成功能
    public async Task<List<string>> GetSymbolSuggestionsAsync(string input, int maxResults = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<string>();

            // 优先使用币安合约服务获取实时建议
            if (_binanceSymbolService != null)
            {
                var suggestions = await _binanceSymbolService.GetSymbolSuggestionsAsync(input, maxResults);
                if (suggestions.Any())
                {
                    Console.WriteLine($"从币安API获取到 {suggestions.Count} 个合约建议");
                    return suggestions;
                }
            }

            // 如果API调用失败，使用本地候选列表
            var localSuggestions = CandidateSymbols
                .Where(c => c.Symbol.Contains(input, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Symbol)
                .Take(maxResults)
                .ToList();

            if (localSuggestions.Any())
            {
                Console.WriteLine($"从本地列表获取到 {localSuggestions.Count} 个合约建议");
                return localSuggestions;
            }

            // 最后使用固定的常见合约列表
            var allSymbols = new List<string>
            {
                "BTCUSDT", "ETHUSDT", "BNBUSDT", "ADAUSDT", "DOTUSDT", "LINKUSDT",
                "LTCUSDT", "BCHUSDT", "XLMUSDT", "EOSUSDT", "TRXUSDT", "ETCUSDT",
                "XRPUSDT", "SOLUSDT", "AVAXUSDT", "MATICUSDT", "ALGOUSDT", "ATOMUSDT",
                "NEARUSDT", "FTMUSDT", "SANDUSDT", "MANAUSDT", "GALAUSDT", "AXSUSDT"
            };

            var fallbackSuggestions = allSymbols
                .Where(s => s.Contains(input.ToUpper()))
                .Take(maxResults)
                .ToList();

            Console.WriteLine($"使用默认列表获取到 {fallbackSuggestions.Count} 个合约建议");
            return fallbackSuggestions;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取合约建议失败: {ex.Message}");
            
            if (_logService != null)
            {
                await _logService.LogErrorAsync("获取合约建议失败", ex, "合约建议");
            }
            
            return new List<string>();
        }
    }

    #endregion

    #region 自动刷新功能

    private void StartAutoRefresh()
    {
        if (_refreshTimer == null)
        {
            _refreshTimer = new System.Threading.Timer(RefreshDataTimerCallback, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            Console.WriteLine("自动刷新已启动，间隔5秒");
        }
        else
        {
            _refreshTimer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            Console.WriteLine("自动刷新已重新启动");
        }
    }

    private void StopAutoRefresh()
    {
        if (_refreshTimer != null)
        {
            _refreshTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            Console.WriteLine("自动刷新已停止");
        }
    }

    private async void RefreshDataTimerCallback(object? state)
    {
        if (!AutoRefreshEnabled || SelectedAccount == null)
            return;

        // 使用信号量避免重复刷新
        if (!await _refreshSemaphore.WaitAsync(0))
        {
            Console.WriteLine("上次刷新未完成，跳过本次刷新");
            return;
        }

        try
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await RefreshAccountAndPositionDataAsync();
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"自动刷新出错: {ex.Message}");
            
            if (_logService != null)
            {
                await _logService.LogErrorAsync("自动刷新失败", ex, "自动刷新");
            }
        }
        finally
        {
            _refreshSemaphore.Release();
        }
    }

    private async Task RefreshAccountAndPositionDataAsync()
    {
        try
        {
            if (SelectedAccount == null || _binanceService == null)
                return;

            // 刷新账户和持仓信息
            await RefreshAccountInfoAsync();
            await RefreshPositionsDataAsync();

            // 更新状态消息
            StatusMessage = $"✅ 数据已刷新 - {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 刷新失败: {ex.Message}";
            throw;
        }
    }

    #endregion

    #region 初始化和数据加载方法

    /// <summary>
    /// 加载初始数据（已保存的账户和合约）
    /// </summary>
    private async Task LoadInitialDataAsync()
    {
        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StatusMessage = "正在加载已保存的账户...";
            });

            // 加载已保存的账户
            if (_configService != null)
            {
                var savedAccounts = await _configService.LoadAccountsAsync();
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (savedAccounts.Count > 0)
                    {
                        // 有已保存的账户，加载它们
                        foreach (var account in savedAccounts)
                        {
                            Accounts.Add(account);
                        }
                        
                        // 自动选择第一个账户
                        SelectedAccount = Accounts[0];
                        
                        StatusMessage = $"✅ 已加载 {savedAccounts.Count} 个账户";
                        
                        if (_logService != null)
                        {
                            _ = Task.Run(async () => await _logService.LogInfoAsync($"加载已保存账户: {savedAccounts.Count}个", "系统启动"));
                        }
                    }
                    else
                    {
                        // 没有已保存的账户，添加模拟数据
                        AddMockData();
                        StatusMessage = "✅ 已加载模拟数据";
                        
                        if (_logService != null)
                        {
                            _ = Task.Run(async () => await _logService.LogInfoAsync("首次启动，加载模拟数据", "系统启动"));
                        }
                    }
                });

                // 加载已保存的候选合约
                var savedSymbols = await _configService.LoadCandidateSymbolsAsync();
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var symbol in savedSymbols)
                    {
                        CandidateSymbols.Add(symbol);
                    }
                });
            }
            else
            {
                // 服务未初始化，添加模拟数据
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AddMockData();
                    StatusMessage = "⚠️ 配置服务未初始化，使用模拟数据";
                });
            }
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 出错时使用模拟数据
                AddMockData();
                StatusMessage = $"❌ 加载账户失败，使用模拟数据: {ex.Message}";
            });
            
            if (_logService != null)
            {
                await _logService.LogErrorAsync("加载初始数据失败", ex, "系统启动");
            }
        }
    }

    #endregion

    #region 账户信息刷新方法
    
    /// <summary>
    /// 刷新账户信息
    /// </summary>
    private async Task RefreshAccountInfoAsync()
    {
        if (SelectedAccount == null || _binanceService == null) return;
        
        try
        {
            var accountInfo = await _binanceService.GetDetailedAccountInfoAsync();
            if (accountInfo != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
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
                    
                    StatusMessage = $"✅ 账户信息已刷新 - 总权益: {SelectedAccount.TotalEquity:F2} USDT";
                });
                
                if (_logService != null)
                {
                    await _logService.LogInfoAsync($"账户信息刷新成功 - 权益: {SelectedAccount.TotalEquity:F2}, 杠杆: {SelectedAccount.Leverage:F2}", "API");
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StatusMessage = $"获取账户信息失败: {ex.Message}";
            });
            
            if (_logService != null)
            {
                await _logService.LogErrorAsync("刷新账户信息失败", ex, "API");
            }
        }
    }
    
    /// <summary>
    /// 刷新持仓信息
    /// </summary>
    private async Task RefreshPositionsDataAsync()
    {
        if (SelectedAccount == null || _binanceService == null) return;
        
        try
        {
            var positions = await _binanceService.GetDetailedPositionsAsync();
            if (positions != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Positions.Clear();
                    foreach (var position in positions.Where(p => Math.Abs(p.PositionAmt) > 0))
                    {
                        Positions.Add(position);
                    }
                    
                    StatusMessage = $"✅ 持仓信息已刷新 - 共{Positions.Count}个持仓";
                });
                
                if (_logService != null)
                {
                    await _logService.LogInfoAsync($"持仓信息刷新成功 - 持仓数量: {Positions.Count}", "API");
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StatusMessage = $"获取持仓信息失败: {ex.Message}";
            });
            
            if (_logService != null)
            {
                await _logService.LogErrorAsync("刷新持仓信息失败", ex, "API");
            }
        }
    }

    /// <summary>
    /// 刷新委托订单信息
    /// </summary>
    private async Task RefreshOpenOrdersAsync()
    {
        if (SelectedAccount == null || _binanceService == null) return;
        
        try
        {
            var openOrders = await _binanceService.GetOpenOrdersAsync();
            if (openOrders != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    OpenOrders.Clear();
                    foreach (var order in openOrders)
                    {
                        // 只显示限价单和止损单（条件单）
                        if (order.Type == "LIMIT" || order.Type == "STOP_MARKET" || order.Type == "STOP")
                        {
                            OpenOrders.Add(order);
                        }
                    }
                    
                    StatusMessage = $"✅ 委托订单已刷新 - 共{OpenOrders.Count}个委托";
                });
                
                if (_logService != null)
                {
                    await _logService.LogInfoAsync($"委托订单刷新成功 - 委托数量: {OpenOrders.Count}", "API");
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StatusMessage = $"获取委托订单失败: {ex.Message}";
            });
            
            if (_logService != null)
            {
                await _logService.LogErrorAsync("刷新委托订单失败", ex, "API");
            }
        }
    }
    
    #endregion
} 