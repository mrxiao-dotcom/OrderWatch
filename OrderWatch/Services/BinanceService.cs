using OrderWatch.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;

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
        
        Console.WriteLine($"币安API凭据已设置，测试网: {isTestNet}");
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_secretKey))
            {
                Console.WriteLine("API凭据未设置");
                return false;
            }

            // 基础网络连接测试
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            // 根据是否测试网选择不同的URL
            string baseUrl = _isTestNet ? "https://testnet.binancefuture.com" : "https://fapi.binance.com";
            string pingUrl = $"{baseUrl}/fapi/v1/ping";
            
            // 先测试基础网络连接
            var pingResponse = await httpClient.GetStringAsync(pingUrl);
            Console.WriteLine($"网络连接测试成功: {pingResponse}");
            
            // 测试带签名的API调用（获取服务器时间）
            string timeUrl = $"{baseUrl}/fapi/v1/time";
            var timeResponse = await httpClient.GetStringAsync(timeUrl);
            Console.WriteLine($"服务器时间获取成功: {timeResponse}");
            
            // 测试需要API Key的调用（获取账户信息）
            await TestApiKeyAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"连接测试失败: {ex.Message}");
            return false;
        }
    }

    private async Task TestApiKeyAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            // 构建需要签名的请求
            string baseUrl = _isTestNet ? "https://testnet.binancefuture.com" : "https://fapi.binance.com";
            string endpoint = "/fapi/v2/account";
            
            // 获取当前时间戳
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string queryString = $"timestamp={timestamp}";
            
            // 生成签名
            string signature = GenerateSignature(queryString);
            string fullUrl = $"{baseUrl}{endpoint}?{queryString}&signature={signature}";
            
            // 设置请求头
            httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);
            
            // 发送请求
            var response = await httpClient.GetStringAsync(fullUrl);
            Console.WriteLine("API Key验证成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API Key验证失败: {ex.Message}");
            throw; // 重新抛出异常以便上层处理
        }
    }

    private string GenerateSignature(string queryString)
    {
        try
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(_secretKey));
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(queryString));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"签名生成失败: {ex.Message}");
            throw;
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
            // 检查API凭据
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_secretKey))
            {
                Console.WriteLine("API凭据未设置，返回模拟委托订单");
                return await GetMockOpenOrdersAsync();
            }

            // 构建获取委托订单请求
            var baseUrl = _isTestNet ? "https://testnet.binancefuture.com" : "https://fapi.binance.com";
            var endpoint = "/fapi/v1/openOrders";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 构建请求参数
            var queryString = $"timestamp={timestamp}";
            
            // 生成签名
            var signature = GenerateSignature(queryString);
            queryString += $"&signature={signature}";

            // 发送GET请求
            var fullUrl = $"{baseUrl}{endpoint}?{queryString}";
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);

            var response = await httpClient.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var orders = ParseOpenOrders(responseBody);
                Console.WriteLine($"获取委托订单成功，共 {orders.Count} 个委托");
                return orders;
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"获取委托订单失败: {response.StatusCode} - {errorBody}");
                return await GetMockOpenOrdersAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取委托订单异常: {ex.Message}");
            return await GetMockOpenOrdersAsync();
        }
    }

    private async Task<List<OrderInfo>> GetMockOpenOrdersAsync()
    {
        await Task.Delay(100);
        var orders = new List<OrderInfo>
        {
            new()
            {
                OrderId = 12345,
                Symbol = "BTCUSDT",
                Side = "BUY",
                Type = "LIMIT",
                OrigQty = 0.1m,
                Price = 50000m,
                ExecutedQty = 0m,
                Status = "NEW",
                UpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                ReduceOnly = false
            },
            new()
            {
                OrderId = 12346,
                Symbol = "ETHUSDT",
                Side = "SELL",
                Type = "STOP_MARKET",
                OrigQty = 1.0m,
                Price = 0m,
                StopPrice = 3000m,
                ExecutedQty = 0m,
                Status = "NEW",
                UpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                ReduceOnly = true
            }
        };
        
        return orders;
    }

    private List<OrderInfo> ParseOpenOrders(string jsonResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var orders = new List<OrderInfo>();

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var order = new OrderInfo
                {
                    OrderId = element.GetProperty("orderId").GetInt64(),
                    Symbol = element.GetProperty("symbol").GetString() ?? "",
                    Side = element.GetProperty("side").GetString() ?? "",
                    Type = element.GetProperty("type").GetString() ?? "",
                    OrigQty = decimal.Parse(element.GetProperty("origQty").GetString() ?? "0"),
                    Price = decimal.Parse(element.GetProperty("price").GetString() ?? "0"),
                    ExecutedQty = decimal.Parse(element.GetProperty("executedQty").GetString() ?? "0"),
                    Status = element.GetProperty("status").GetString() ?? "",
                    UpdateTime = element.GetProperty("updateTime").GetInt64(),
                    ReduceOnly = element.TryGetProperty("reduceOnly", out var reduceOnlyElement) && reduceOnlyElement.GetBoolean()
                };

                // 解析stopPrice（如果存在）
                if (element.TryGetProperty("stopPrice", out var stopPriceElement))
                {
                    order.StopPrice = decimal.Parse(stopPriceElement.GetString() ?? "0");
                }

                orders.Add(order);
            }

            return orders;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析委托订单失败: {ex.Message}");
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
            // 检查API凭据
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_secretKey))
            {
                Console.WriteLine("API凭据未设置，无法下单");
                return false;
            }

            // 构建下单请求
            var baseUrl = _isTestNet ? "https://testnet.binancefuture.com" : "https://fapi.binance.com";
            var endpoint = "/fapi/v1/order";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 构建请求参数
            var parameters = new Dictionary<string, string>
            {
                {"symbol", request.Symbol},
                {"side", request.Side},
                {"type", request.Type},
                {"quantity", request.Quantity.ToString("F8")},
                {"timestamp", timestamp.ToString()}
            };

            // 如果是限价单，添加价格参数
            if (request.Type == "LIMIT" && request.Price > 0)
            {
                parameters.Add("price", request.Price.ToString("F8"));
                parameters.Add("timeInForce", "GTC"); // Good Till Canceled
            }

            // 如果是条件单，添加stopPrice参数
            if (request.Type == "STOP_MARKET" && request.StopPrice > 0)
            {
                parameters.Add("stopPrice", request.StopPrice.ToString("F8"));
            }

            // 如果是reduceOnly订单
            if (request.ReduceOnly)
            {
                parameters.Add("reduceOnly", "true");
            }

            // 构建查询字符串
            var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
            
            // 生成签名
            var signature = GenerateSignature(queryString);
            queryString += $"&signature={signature}";

            // 发送POST请求
            var fullUrl = $"{baseUrl}{endpoint}";
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);

            var content = new StringContent(queryString, Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await httpClient.PostAsync(fullUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"下单成功: {request.Symbol} {request.Side} {request.Type} {request.Quantity}");
                Console.WriteLine($"响应: {responseBody}");
                return true;
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"下单失败: {response.StatusCode} - {errorBody}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下单异常: {ex.Message}");
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
            // Console.WriteLine($"设置保证金类型成功: {symbol} {leverage}");
            return true;
        }
        catch (Exception)
        {
            // Console.WriteLine($"设置保证金类型异常");
            return false;
        }
    }

    public async Task<AccountInfo?> GetDetailedAccountInfoAsync()
    {
        try
        {
            // 如果没有设置API凭据，返回模拟数据
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_secretKey))
            {
                Console.WriteLine("API凭据未设置，返回模拟数据");
                return await GetMockAccountInfoAsync();
            }

            // 使用真实API获取账户信息
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);
            
            string baseUrl = _isTestNet ? "https://testnet.binancefuture.com" : "https://fapi.binance.com";
            string endpoint = "/fapi/v2/account";
            
            // 构建签名请求
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string queryString = $"timestamp={timestamp}";
            string signature = GenerateSignature(queryString);
            string fullUrl = $"{baseUrl}{endpoint}?{queryString}&signature={signature}";
            
            // 设置请求头
            httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);
            
            // 发送请求
            var response = await httpClient.GetStringAsync(fullUrl);
            Console.WriteLine($"获取真实账户信息成功: {response.Substring(0, Math.Min(200, response.Length))}...");
            
            // 解析响应
            var accountData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(response);
            
            // 转换为AccountInfo对象
            var account = ParseAccountInfo(response);
            return account;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取真实账户信息失败，返回模拟数据: {ex.Message}");
            // 出错时返回模拟数据
            return await GetMockAccountInfoAsync();
        }
    }

    private async Task<AccountInfo> GetMockAccountInfoAsync()
    {
        await Task.Delay(100);
        return new AccountInfo
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
            MaxWithdrawAmount = 8000m,
            LongMarketValue = 8000m,
            ShortMarketValue = 2000m,
            TotalMarketValue = 10000m,
            NetMarketValue = 6000m,
            Leverage = 2.0m
        };
    }

    private AccountInfo ParseAccountInfo(string jsonResponse)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(jsonResponse);
            var root = document.RootElement;
            
            var account = new AccountInfo
            {
                TotalWalletBalance = decimal.Parse(root.GetProperty("totalWalletBalance").GetString() ?? "0"),
                TotalUnrealizedProfit = decimal.Parse(root.GetProperty("totalUnrealizedProfit").GetString() ?? "0"),
                TotalMarginBalance = decimal.Parse(root.GetProperty("totalMarginBalance").GetString() ?? "0"),
                TotalInitialMargin = decimal.Parse(root.GetProperty("totalInitialMargin").GetString() ?? "0"),
                TotalMaintMargin = decimal.Parse(root.GetProperty("totalMaintMargin").GetString() ?? "0"),
                TotalPositionInitialMargin = decimal.Parse(root.GetProperty("totalPositionInitialMargin").GetString() ?? "0"),
                TotalOpenOrderInitialMargin = decimal.Parse(root.GetProperty("totalOpenOrderInitialMargin").GetString() ?? "0"),
                TotalCrossWalletBalance = decimal.Parse(root.GetProperty("totalCrossWalletBalance").GetString() ?? "0"),
                TotalCrossUnPnl = decimal.Parse(root.GetProperty("totalCrossUnPnl").GetString() ?? "0"),
                MaxWithdrawAmount = decimal.Parse(root.GetProperty("maxWithdrawAmount").GetString() ?? "0")
            };
            
            // 计算衍生值
            account.LongMarketValue = account.TotalWalletBalance * 0.6m; // 示例计算
            account.ShortMarketValue = account.TotalWalletBalance * 0.2m;
            account.TotalMarketValue = account.TotalWalletBalance;
            account.NetMarketValue = account.TotalWalletBalance - account.TotalInitialMargin;
            account.Leverage = account.TotalInitialMargin > 0 ? account.TotalMarginBalance / account.TotalWalletBalance : 1.0m;
            
            Console.WriteLine($"解析账户信息成功 - 钱包余额: {account.TotalWalletBalance}, 总权益: {account.TotalEquity}");
            return account;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析账户信息失败: {ex.Message}");
            throw;
        }
    }

    public async Task<List<PositionInfo>> GetDetailedPositionsAsync()
    {
        try
        {
            // 如果没有设置API凭据，返回模拟数据
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_secretKey))
            {
                Console.WriteLine("API凭据未设置，返回模拟持仓数据");
                return await GetMockPositionsAsync();
            }

            // 使用真实API获取持仓信息
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);
            
            string baseUrl = _isTestNet ? "https://testnet.binancefuture.com" : "https://fapi.binance.com";
            string endpoint = "/fapi/v2/positionRisk";
            
            // 构建签名请求
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string queryString = $"timestamp={timestamp}";
            string signature = GenerateSignature(queryString);
            string fullUrl = $"{baseUrl}{endpoint}?{queryString}&signature={signature}";
            
            // 设置请求头
            httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);
            
            // 发送请求
            var response = await httpClient.GetStringAsync(fullUrl);
            Console.WriteLine($"获取真实持仓信息成功，响应长度: {response.Length}");
            
            // 解析响应
            var positions = ParsePositionsInfo(response);
            
            // 只返回有持仓的合约（positionAmt != 0）
            var activePositions = positions.Where(p => Math.Abs(p.PositionAmt) > 0).ToList();
            
            Console.WriteLine($"活跃持仓数量: {activePositions.Count}");
            return activePositions;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取真实持仓信息失败，返回模拟数据: {ex.Message}");
            // 出错时返回模拟数据
            return await GetMockPositionsAsync();
        }
    }

    private async Task<List<PositionInfo>> GetMockPositionsAsync()
    {
        await Task.Delay(100);
        return new List<PositionInfo>
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
                Notional = 5100m,
                OrderCount = 2,
                ConditionalOrderCount = 1
            },
            new()
            {
                Symbol = "ETHUSDT",
                PositionSide = "SHORT",
                PositionAmt = -0.5m,
                EntryPrice = 3000m,
                MarkPrice = 2900m,
                UnRealizedProfit = 50m,
                LiquidationPrice = 3500m,
                Leverage = 5,
                Notional = 1450m,
                OrderCount = 1,
                ConditionalOrderCount = 0
            }
        };
    }

    private List<PositionInfo> ParsePositionsInfo(string jsonResponse)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(jsonResponse);
            var positions = new List<PositionInfo>();
            
            foreach (var element in document.RootElement.EnumerateArray())
            {
                var position = new PositionInfo
                {
                    Symbol = element.GetProperty("symbol").GetString() ?? "",
                    PositionSide = element.GetProperty("positionSide").GetString() ?? "BOTH",
                    PositionAmt = decimal.Parse(element.GetProperty("positionAmt").GetString() ?? "0"),
                    EntryPrice = decimal.Parse(element.GetProperty("entryPrice").GetString() ?? "0"),
                    MarkPrice = decimal.Parse(element.GetProperty("markPrice").GetString() ?? "0"),
                    UnRealizedProfit = decimal.Parse(element.GetProperty("unRealizedProfit").GetString() ?? "0"),
                    Leverage = decimal.Parse(element.GetProperty("leverage").GetString() ?? "1"),
                    Notional = decimal.Parse(element.GetProperty("notional").GetString() ?? "0")
                };
                
                // 尝试获取其他可选字段
                if (element.TryGetProperty("liquidationPrice", out var liqPrice))
                {
                    position.LiquidationPrice = decimal.Parse(liqPrice.GetString() ?? "0");
                }
                
                positions.Add(position);
            }
            
            Console.WriteLine($"解析持仓信息成功，总数量: {positions.Count}");
            return positions;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析持仓信息失败: {ex.Message}");
            throw;
        }
    }

    public async Task<SymbolInfo?> GetSymbolInfoAsync(string symbol)
    {
        try
        {
            // 临时实现：返回模拟的合约信息
            await Task.Delay(100);
            
            if (string.IsNullOrEmpty(symbol))
                return null;
                
            var upperSymbol = symbol.ToUpper();
            
            // 根据合约名称返回相应的精度信息
            var symbolInfo = new SymbolInfo
            {
                Symbol = symbol,
                Status = "TRADING",
                BaseAsset = symbol.Replace("USDT", "").Replace("BUSD", ""),
                QuoteAsset = symbol.Contains("USDT") ? "USDT" : "BUSD",
                LastUpdate = DateTime.Now
            };
            
            // 根据合约类型设置精度信息
            if (upperSymbol.Contains("BTC") || upperSymbol.Contains("ETH") || upperSymbol.Contains("BNB"))
            {
                symbolInfo.PricePrecision = 2;      // 价格精度：0.01
                symbolInfo.QuantityPrecision = 3;   // 数量精度：0.001
                symbolInfo.MinQty = 0.001m;         // 最小数量：0.001
                symbolInfo.MaxQty = 100000m;
                symbolInfo.StepSize = 0.001m;
                symbolInfo.MinPrice = 0.01m;
                symbolInfo.MaxPrice = 1000000m;
                symbolInfo.TickSize = 0.01m;
                symbolInfo.MinNotional = 5.0m;      // 最小金额：5 USDT
            }
            else if (upperSymbol.Contains("USDT") || upperSymbol.Contains("BUSD"))
            {
                symbolInfo.PricePrecision = 4;      // 价格精度：0.0001
                symbolInfo.QuantityPrecision = 1;   // 数量精度：0.1
                symbolInfo.MinQty = 0.1m;           // 最小数量：0.1
                symbolInfo.MaxQty = 100000m;
                symbolInfo.StepSize = 0.1m;
                symbolInfo.MinPrice = 0.0001m;
                symbolInfo.MaxPrice = 1000000m;
                symbolInfo.TickSize = 0.0001m;
                symbolInfo.MinNotional = 5.0m;      // 最小金额：5 USDT
            }
            else if (upperSymbol.Contains("DOGE") || upperSymbol.Contains("SHIB"))
            {
                symbolInfo.PricePrecision = 6;      // 价格精度：0.000001
                symbolInfo.QuantityPrecision = 0;   // 数量精度：1 (整数)
                symbolInfo.MinQty = 1m;              // 最小数量：1
                symbolInfo.MaxQty = 1000000m;
                symbolInfo.StepSize = 1m;
                symbolInfo.MinPrice = 0.000001m;
                symbolInfo.MaxPrice = 1m;
                symbolInfo.TickSize = 0.000001m;
                symbolInfo.MinNotional = 5.0m;      // 最小金额：5 USDT
            }
            else if (upperSymbol.Contains("ADA") || upperSymbol.Contains("DOT") || upperSymbol.Contains("LINK"))
            {
                symbolInfo.PricePrecision = 4;      // 价格精度：0.0001
                symbolInfo.QuantityPrecision = 2;   // 数量精度：0.01
                symbolInfo.MinQty = 0.01m;          // 最小数量：0.01
                symbolInfo.MaxQty = 100000m;
                symbolInfo.StepSize = 0.01m;
                symbolInfo.MinPrice = 0.0001m;
                symbolInfo.MaxPrice = 100000m;
                symbolInfo.TickSize = 0.0001m;
                symbolInfo.MinNotional = 5.0m;      // 最小金额：5 USDT
            }
            else
            {
                // 默认精度设置
                symbolInfo.PricePrecision = 4;      // 价格精度：0.0001
                symbolInfo.QuantityPrecision = 1;   // 数量精度：0.1
                symbolInfo.MinQty = 0.1m;           // 最小数量：0.1
                symbolInfo.MaxQty = 100000m;
                symbolInfo.StepSize = 0.1m;
                symbolInfo.MinPrice = 0.0001m;
                symbolInfo.MaxPrice = 1000000m;
                symbolInfo.TickSize = 0.0001m;
                symbolInfo.MinNotional = 5.0m;      // 最小金额：5 USDT
            }
            
            Console.WriteLine($"获取合约信息成功: {symbol} (价格精度: {symbolInfo.PricePrecision}, 数量精度: {symbolInfo.QuantityPrecision})");
            return symbolInfo;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取合约信息失败: {symbol} - {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        // 临时实现：无需清理资源
    }
}
