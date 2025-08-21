using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OrderWatch.Models;
using OrderWatch.Services;
using OrderWatch.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace OrderWatch.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IBinanceService _binanceService;
    private readonly IConfigService _configService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly Timer _refreshTimer;

    public MainViewModel(IBinanceService binanceService, IConfigService configService, ILogger<MainViewModel> logger)
    {
        _binanceService = binanceService;
        _configService = configService;
        _logger = logger;

        // 初始化集合
        Accounts = new ObservableCollection<AccountInfo>();
        Positions = new ObservableCollection<PositionInfo>();
        Orders = new ObservableCollection<OrderInfo>();
        CandidateSymbols = new ObservableCollection<CandidateSymbol>();

        // 初始化定时器
        _refreshTimer = new Timer(async _ => await RefreshDataAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

        // 加载初始数据
        _ = Task.Run(async () => await LoadInitialDataAsync());
    }

    #region 属性

    [ObservableProperty]
    private AccountInfo? _selectedAccount;

    [ObservableProperty]
    private PositionInfo? _selectedPosition;

    [ObservableProperty]
    private OrderInfo? _selectedOrder;

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

    #endregion

    #region 集合

    public ObservableCollection<AccountInfo> Accounts { get; }
    public ObservableCollection<PositionInfo> Positions { get; }
    public ObservableCollection<OrderInfo> Orders { get; }
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
                RefreshOrdersAsync()
            };

            await Task.WhenAll(tasks);

            StatusMessage = $"数据已刷新 - {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新数据失败");
            StatusMessage = $"刷新失败: {ex.Message}";
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

            StatusMessage = "初始数据加载完成";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载初始数据失败");
            StatusMessage = $"加载失败: {ex.Message}";
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
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下单异常");
            StatusMessage = $"下单异常: {ex.Message}";
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
                await RefreshDataAsync();
            }
            else
            {
                StatusMessage = "订单取消失败";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消订单异常");
            StatusMessage = $"取消异常: {ex.Message}";
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加候选币失败");
            StatusMessage = $"添加失败: {ex.Message}";
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除候选币失败");
            StatusMessage = $"删除失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SelectCandidateSymbolAsync()
    {
        if (SelectedCandidateSymbol == null) return;

        Symbol = SelectedCandidateSymbol.Symbol;
        await UpdateLatestPriceAsync();
        StatusMessage = $"已选择: {SelectedCandidateSymbol.Symbol}";
    }

    [RelayCommand]
    private async Task AddAccountAsync()
    {
        try
        {
            var accountWindow = new Views.AccountConfigWindow();
            if (accountWindow.ShowDialog() == true)
            {
                await _configService.SaveAccountAsync(accountWindow.AccountInfo);
                Accounts.Add(accountWindow.AccountInfo);
                StatusMessage = $"已添加账户: {accountWindow.AccountInfo.Name}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加账户失败");
            StatusMessage = $"添加账户失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task EditAccountAsync()
    {
        if (SelectedAccount == null) return;

        try
        {
            var accountWindow = new Views.AccountConfigWindow(SelectedAccount);
            if (accountWindow.ShowDialog() == true)
            {
                await _configService.SaveAccountAsync(accountWindow.AccountInfo);
                StatusMessage = $"已更新账户: {accountWindow.AccountInfo.Name}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "编辑账户失败");
            StatusMessage = $"编辑账户失败: {ex.Message}";
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
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除账户失败");
            StatusMessage = $"删除账户失败: {ex.Message}";
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
            _logger.LogError(ex, "刷新账户信息失败");
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
            _logger.LogError(ex, "刷新持仓信息失败");
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
            _logger.LogError(ex, "刷新订单信息失败");
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
            _logger.LogError(ex, "更新最新价格失败");
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
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建止损单失败");
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
            _ = Task.Run(async () => await UpdateLatestPriceAsync());
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

    public void Dispose()
    {
        _refreshTimer?.Dispose();
    }
}
