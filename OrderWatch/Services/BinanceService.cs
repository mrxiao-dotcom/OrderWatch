using OrderWatch.Models;
using System.Net.Http;

namespace OrderWatch.Services;

public class BinanceService : IBinanceService, IDisposable
{
    private string _apiKey = string.Empty;
    private string _secretKey = string.Empty;
    private bool _isTestNet;

    public BinanceService()
    {
    }

    public void SetCredentials(string apiKey, string secretKey, bool isTestNet)
    {
        _apiKey = apiKey;
        _secretKey = secretKey;
        _isTestNet = isTestNet;
        
        // Console.WriteLine($"币安API凭据已设置，测试网: {isTestNet}");
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            // 测试网络连接
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            var response = await httpClient.GetStringAsync("https://fapi.binance.com/fapi/v1/ping");
            Console.WriteLine($"网络连接测试成功: {response}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"网络连接测试失败: {ex.Message}");
            return false;
        }
    }

    public async Task<AccountInfo?> GetAccountInfoAsync()
    {
        try
        {
            // 临时实现：返回模拟数据
            await Task.Delay(100);
            var account = new AccountInfo
            {
                TotalWalletBalance = 10000m,
                TotalUnrealizedProfit = 500m,
                TotalMarginBalance = 10500m,
                TotalInitialMargin = 2000m,
                TotalMaintMargin = 100m,
                TotalPositionInitialMargin = 1500m,
                TotalOpenOrderInitialMargin = 500m,
                TotalCrossWalletBalance = 10500m,
                TotalCrossUnPnl = 500m,
                MaxWithdrawAmount = 8000m
            };
            
            // Console.WriteLine("获取账户信息成功");
            return account;
        }
        catch (Exception)
        {
            // Console.WriteLine($"获取账户信息失败");
            return null;
        }
    }

    public async Task<List<PositionInfo>> GetPositionsAsync()
    {
        try
        {
            // 临时实现：返回模拟数据
            await Task.Delay(100);
            var positions = new List<PositionInfo>
            {
                new()
                {
                    Symbol = "BTCUSDT",
                    PositionSide = "LONG",
                    PositionAmt = 0.1m,
                    EntryPrice = 50000m,
                    MarkPrice = 51000m,
                    UnRealizedProfit = 100m,
                    LiquidationPrice = 45000m,
                    Leverage = 10,
                    Notional = 5100m
                }
            };
            
            // Console.WriteLine($"获取持仓信息成功，共 {positions.Count} 个持仓");
            return positions;
        }
        catch (Exception)
        {
            // Console.WriteLine($"获取持仓信息失败");
            return new List<PositionInfo>();
        }
    }

    public async Task<List<OrderInfo>> GetOpenOrdersAsync()
    {
        try
        {
            // 临时实现：返回模拟数据
            await Task.Delay(100);
            var orders = new List<OrderInfo>
            {
                new()
                {
                    OrderId = 12345,
                    Symbol = "BTCUSDT",
                    Side = "BUY",
                    Type = "LIMIT",
                    Quantity = 0.1m,
                    Price = 50000m,
                    ExecutedQty = 0m,
                    Status = "NEW",
                    UpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                }
            };
            
            // Console.WriteLine($"获取委托单成功，共 {orders.Count} 个委托");
            return orders;
        }
        catch (Exception)
        {
            // Console.WriteLine($"获取委托单失败");
            return new List<OrderInfo>();
        }
    }

    public async Task<List<OrderInfo>> GetAllOrdersAsync(string symbol, long? orderId = null, int? limit = null)
    {
        try
        {
            // 临时实现：返回模拟数据
            await Task.Delay(100);
            var orders = new List<OrderInfo>
            {
                new()
                {
                    OrderId = 12345,
                    Symbol = symbol,
                    Side = "BUY",
                    Type = "LIMIT",
                    Quantity = 0.1m,
                    Price = 50000m,
                    ExecutedQty = 0.1m,
                    Status = "FILLED",
                    UpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                }
            };
            
            // Console.WriteLine($"获取历史订单成功，共 {orders.Count} 个订单");
            return orders;
        }
        catch (Exception)
        {
            // Console.WriteLine($"获取历史订单失败");
            return new List<OrderInfo>();
        }
    }

    public async Task<bool> PlaceOrderAsync(TradingRequest request)
    {
        try
        {
            // 临时实现：模拟下单成功
            await Task.Delay(100);
            // Console.WriteLine($"下单成功: {request.Symbol} {request.Side} {request.Type} {request.Quantity}");
            return true;
        }
        catch (Exception)
        {
            // Console.WriteLine($"下单异常");
            return false;
        }
    }

    public async Task<bool> CancelOrderAsync(string symbol, long orderId)
    {
        try
        {
            // 临时实现：模拟撤单成功
            await Task.Delay(100);
            // Console.WriteLine($"撤单成功: {symbol} {orderId}");
            return true;
        }
        catch (Exception)
        {
            // Console.WriteLine($"撤单异常");
            return false;
        }
    }

    public async Task<bool> CancelAllOrdersAsync(string symbol)
    {
        try
        {
            // 临时实现：模拟撤销所有订单成功
            await Task.Delay(100);
            // Console.WriteLine($"撤销所有订单成功: {symbol}");
            return true;
        }
        catch (Exception)
        {
            // Console.WriteLine($"撤销所有订单异常");
            return false;
        }
    }

    public async Task<decimal> GetLatestPriceAsync(string symbol)
    {
        try
        {
            // 使用 HTTP 客户端获取币安API实时价格
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10); // 设置超时时间
            
            var url = $"https://fapi.binance.com/fapi/v1/ticker/price?symbol={symbol}";
            Console.WriteLine($"正在获取 {symbol} 的最新价格: {url}");
            
            var response = await httpClient.GetStringAsync(url);
            Console.WriteLine($"API响应: {response}");
            
            // 简单的 JSON 解析
            if (response.Contains("\"price\""))
            {
                var priceIndex = response.IndexOf("\"price\":\"") + 9;
                var endIndex = response.IndexOf("\"", priceIndex);
                if (priceIndex > 8 && endIndex > priceIndex)
                {
                    var priceStr = response.Substring(priceIndex, endIndex - priceIndex);
                    if (decimal.TryParse(priceStr, out decimal price))
                    {
                        Console.WriteLine($"成功解析价格: {symbol} = {price}");
                        return price;
                    }
                }
            }
            
            Console.WriteLine($"JSON解析失败: {symbol}");
            // 如果解析失败，返回0而不是硬编码的备用价格
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取 {symbol} 价格异常: {ex.Message}");
            // 异常情况下返回0，让调用方知道获取失败
            return 0;
        }
    }

    public async Task<decimal> Get24hrPriceChangeAsync(string symbol)
    {
        try
        {
            // 使用 HTTP 客户端获取24小时价格变化
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10); // 设置超时时间
            
            var url = $"https://fapi.binance.com/fapi/v1/ticker/24hr?symbol={symbol}";
            Console.WriteLine($"正在获取 {symbol} 的24小时价格变化: {url}");
            
            var response = await httpClient.GetStringAsync(url);
            Console.WriteLine($"24hr API响应: {response}");
            
            // 简单的 JSON 解析
            if (response.Contains("\"priceChangePercent\""))
            {
                var changeIndex = response.IndexOf("\"priceChangePercent\":\"") + 22;
                var endIndex = response.IndexOf("\"", changeIndex);
                if (changeIndex > 21 && endIndex > changeIndex)
                {
                    var changeStr = response.Substring(changeIndex, endIndex - changeIndex);
                    if (decimal.TryParse(changeStr, out decimal changePercent))
                    {
                        Console.WriteLine($"成功解析24hr变化: {symbol} = {changePercent}%");
                        return changePercent;
                    }
                }
            }
            
            Console.WriteLine($"24hr JSON解析失败: {symbol}");
            // 如果解析失败，返回0而不是硬编码的备用数据
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取 {symbol} 24hr变化异常: {ex.Message}");
            // 异常情况下返回0，让调用方知道获取失败
            return 0;
        }
    }

    public async Task<bool> SetLeverageAsync(string symbol, int leverage)
    {
        try
        {
            // 临时实现：模拟设置杠杆成功
            await Task.Delay(100);
            // Console.WriteLine($"设置杠杆成功: {symbol} {leverage}");
            return true;
        }
        catch (Exception)
        {
            // Console.WriteLine($"设置杠杆异常");
            return false;
        }
    }

    public async Task<bool> SetMarginTypeAsync(string symbol, string marginType)
    {
        try
        {
            // 临时实现：模拟设置保证金类型成功
            await Task.Delay(100);
            // Console.WriteLine($"设置保证金类型成功: {symbol} {marginType}");
            return true;
        }
        catch (Exception)
        {
            // Console.WriteLine($"设置保证金类型异常");
            return false;
        }
    }

    public void Dispose()
    {
        // 临时实现：无需清理资源
    }
}
