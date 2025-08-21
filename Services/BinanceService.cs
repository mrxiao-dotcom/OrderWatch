using Binance.Net.Clients;
using Binance.Net.Objects;
using Microsoft.Extensions.Logging;
using OrderWatch.Models;

namespace OrderWatch.Services;

public class BinanceService : IBinanceService, IDisposable
{
    private readonly BinanceClient _client;
    private readonly BinanceSocketClient _socketClient;
    private readonly ILogger<BinanceService> _logger;
    private string _apiKey = string.Empty;
    private string _secretKey = string.Empty;
    private bool _isTestNet;

    public BinanceService(ILogger<BinanceService> logger)
    {
        _logger = logger;
        _client = new BinanceClient();
        _socketClient = new BinanceSocketClient();
    }

    public void SetCredentials(string apiKey, string secretKey, bool isTestNet)
    {
        _apiKey = apiKey;
        _secretKey = secretKey;
        _isTestNet = isTestNet;

        var baseUrl = isTestNet ? "https://testnet.binancefuture.com" : "https://fapi.binance.com";
        
        var options = new BinanceClientOptions
        {
            ApiCredentials = new BinanceApiCredentials(apiKey, secretKey),
            UsdFuturesApiOptions = new BinanceApiClientOptions
            {
                BaseAddress = baseUrl
            }
        };

        _client.Dispose(); // Dispose existing client
        _client = new BinanceClient(options); // Re-initialize with new credentials
        
        _logger.LogInformation("币安API凭据已设置，测试网: {IsTestNet}", isTestNet);
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var result = await _client.UsdFuturesApi.Account.GetAccountInfoAsync();
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试币安连接失败");
            return false;
        }
    }

    public async Task<AccountInfo?> GetAccountInfoAsync()
    {
        try
        {
            var result = await _client.UsdFuturesApi.Account.GetAccountInfoAsync();
            if (!result.Success) return null;

            var account = result.Data;
            return new AccountInfo
            {
                TotalWalletBalance = account.TotalWalletBalance,
                TotalUnrealizedProfit = account.TotalUnrealizedProfit,
                TotalMarginBalance = account.TotalMarginBalance,
                TotalInitialMargin = account.TotalInitialMargin,
                TotalMaintMargin = account.TotalMaintMargin,
                TotalPositionInitialMargin = account.TotalPositionInitialMargin,
                TotalOpenOrderInitialMargin = account.TotalOpenOrderInitialMargin,
                TotalCrossWalletBalance = account.TotalCrossWalletBalance,
                TotalCrossUnPnl = account.TotalCrossUnPnl,
                AvailableBalance = account.AvailableBalance,
                MaxWithdrawAmount = account.MaxWithdrawAmount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取账户信息失败");
            return null;
        }
    }

    public async Task<List<PositionInfo>> GetPositionsAsync()
    {
        try
        {
            var result = await _client.UsdFuturesApi.Account.GetPositionInformationAsync();
            if (!result.Success) return new List<PositionInfo>();

            return result.Data
                .Where(p => p.PositionAmt != 0)
                .Select(p => new PositionInfo
                {
                    Symbol = p.Symbol,
                    PositionSide = p.PositionSide.ToString(),
                    PositionAmt = p.PositionAmt,
                    EntryPrice = p.EntryPrice,
                    MarkPrice = p.MarkPrice,
                    UnRealizedProfit = p.UnrealizedPnl,
                    LiquidationPrice = p.LiquidationPrice,
                    Leverage = p.Leverage,
                    Notional = p.Notional
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取持仓信息失败");
            return new List<PositionInfo>();
        }
    }

    public async Task<List<OrderInfo>> GetOpenOrdersAsync()
    {
        try
        {
            var result = await _client.UsdFuturesApi.Trading.GetOpenOrdersAsync();
            if (!result.Success) return new List<OrderInfo>();

            return result.Data.Select(o => new OrderInfo
            {
                OrderId = o.Id,
                Symbol = o.Symbol,
                Side = o.Side.ToString(),
                Type = o.Type.ToString(),
                Quantity = o.Quantity,
                Price = o.Price,
                ExecutedQty = o.QuantityFilled,
                Status = o.Status.ToString(),
                UpdateTime = o.UpdateTime.ToUnixTimeMilliseconds()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取委托单失败");
            return new List<OrderInfo>();
        }
    }

    public async Task<List<OrderInfo>> GetAllOrdersAsync(string symbol, long? orderId = null, int? limit = null)
    {
        try
        {
            var result = await _client.UsdFuturesApi.Trading.GetOrdersAsync(symbol, orderId, limit);
            if (!result.Success) return new List<OrderInfo>();

            return result.Data.Select(o => new OrderInfo
            {
                OrderId = o.Id,
                Symbol = o.Symbol,
                Side = o.Side.ToString(),
                Type = o.Type.ToString(),
                Quantity = o.Quantity,
                Price = o.Price,
                ExecutedQty = o.QuantityFilled,
                Status = o.Status.ToString(),
                UpdateTime = o.UpdateTime.ToUnixTimeMilliseconds()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取历史订单失败");
            return new List<OrderInfo>();
        }
    }

    public async Task<bool> PlaceOrderAsync(TradingRequest request)
    {
        try
        {
            var orderRequest = new Binance.Net.Objects.Models.Futures.FuturesOrderRequest
            {
                Symbol = request.Symbol,
                Side = Enum.Parse<Binance.Net.Enums.OrderSide>(request.Side),
                Type = Enum.Parse<Binance.Net.Enums.FuturesOrderType>(request.Type),
                Quantity = request.Quantity,
                Price = request.Price,
                StopPrice = request.StopPrice > 0 ? request.StopPrice : null,
                TimeInForce = Enum.Parse<Binance.Net.Enums.TimeInForce>(request.TimeInForce),
                ReduceOnly = request.ReduceOnly,
                PositionSide = Enum.Parse<Binance.Net.Enums.PositionSide>(request.PositionSide),
                WorkingType = Enum.Parse<Binance.Net.Enums.WorkingType>(request.WorkingType),
                ClosePosition = request.ClosePosition,
                ActivationPrice = request.ActivationPrice,
                CallbackRate = request.CallbackRate,
                PriceProtect = request.PriceProtect == "TRUE"
            };

            var result = await _client.UsdFuturesApi.Trading.PlaceOrderAsync(orderRequest);
            if (result.Success)
            {
                _logger.LogInformation("下单成功: {Symbol} {Side} {Type} {Quantity}", 
                    request.Symbol, request.Side, request.Type, request.Quantity);
                return true;
            }
            else
            {
                _logger.LogError("下单失败: {Error}", result.Error?.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下单异常");
            return false;
        }
    }

    public async Task<bool> CancelOrderAsync(string symbol, long orderId)
    {
        try
        {
            var result = await _client.UsdFuturesApi.Trading.CancelOrderAsync(symbol, orderId);
            if (result.Success)
            {
                _logger.LogInformation("撤单成功: {Symbol} {OrderId}", symbol, orderId);
                return true;
            }
            else
            {
                _logger.LogError("撤单失败: {Error}", result.Error?.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "撤单异常");
            return false;
        }
    }

    public async Task<bool> CancelAllOrdersAsync(string symbol)
    {
        try
        {
            var result = await _client.UsdFuturesApi.Trading.CancelAllOrdersAsync(symbol);
            if (result.Success)
            {
                _logger.LogInformation("撤销所有订单成功: {Symbol}", symbol);
                return true;
            }
            else
            {
                _logger.LogError("撤销所有订单失败: {Error}", result.Error?.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "撤销所有订单异常");
            return false;
        }
    }

    public async Task<decimal> GetLatestPriceAsync(string symbol)
    {
        try
        {
            var result = await _client.UsdFuturesApi.ExchangeData.GetPriceAsync(symbol);
            return result.Success ? result.Data.Price : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最新价格失败: {Symbol}", symbol);
            return 0;
        }
    }

    public async Task<decimal> Get24hrPriceChangeAsync(string symbol)
    {
        try
        {
            var result = await _client.UsdFuturesApi.ExchangeData.Get24HourPriceChangeAsync(symbol);
            return result.Success ? result.Data.PriceChangePercent : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取24小时价格变化失败: {Symbol}", symbol);
            return 0;
        }
    }

    public async Task<bool> SetLeverageAsync(string symbol, int leverage)
    {
        try
        {
            var result = await _client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(symbol, leverage);
            if (result.Success)
            {
                _logger.LogInformation("设置杠杆成功: {Symbol} {Leverage}", symbol, leverage);
                return true;
            }
            else
            {
                _logger.LogError("设置杠杆失败: {Error}", result.Error?.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置杠杆异常");
            return false;
        }
    }

    public async Task<bool> SetMarginTypeAsync(string symbol, string marginType)
    {
        try
        {
            var marginTypeEnum = Enum.Parse<Binance.Net.Enums.FuturesMarginType>(marginType);
            var result = await _client.UsdFuturesApi.Account.ChangeMarginTypeAsync(symbol, marginTypeEnum);
            if (result.Success)
            {
                _logger.LogInformation("设置保证金类型成功: {Symbol} {MarginType}", symbol, marginType);
                return true;
            }
            else
            {
                _logger.LogError("设置保证金类型失败: {Error}", result.Error?.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置保证金类型异常");
            return false;
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
        _socketClient?.Dispose();
    }
}
