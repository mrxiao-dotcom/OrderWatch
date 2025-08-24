using System.Text.Json;
using OrderWatch.Models;
using System.IO;

namespace OrderWatch.Services;

public class ConditionalOrderService : IConditionalOrderService, IDisposable
{
    private readonly IBinanceService _binanceService;
    private readonly string _configPath;
    private readonly List<ConditionalOrder> _conditionalOrders;
    private long _nextId = 1;
    private Timer? _monitorTimer;
    private readonly SemaphoreSlim _executionSemaphore = new(1, 1);
    private bool _disposed = false;

    public ConditionalOrderService(IBinanceService binanceService)
    {
        _binanceService = binanceService;
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "OrderWatch", 
            "conditional_orders.json");
        
        _conditionalOrders = LoadConditionalOrders();
        _nextId = _conditionalOrders.Count > 0 ? _conditionalOrders.Max(o => o.Id) + 1 : 1;
        
        // 不在构造函数中启动定时器，由外部调用StartConditionalOrderMonitor()
    }

    public async Task<List<ConditionalOrder>> GetConditionalOrdersAsync()
    {
        return await Task.FromResult(_conditionalOrders.ToList());
    }

    public async Task<ConditionalOrder?> GetConditionalOrderByIdAsync(long id)
    {
        return await Task.FromResult(_conditionalOrders.FirstOrDefault(o => o.Id == id));
    }

    public async Task<bool> CreateConditionalOrderAsync(ConditionalOrder order)
    {
        try
        {
            order.Id = _nextId++;
            order.CreateTime = DateTime.Now;
            order.Status = "PENDING";
            
            _conditionalOrders.Add(order);
            await SaveConditionalOrdersAsync();
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> UpdateConditionalOrderAsync(ConditionalOrder order)
    {
        try
        {
            var existingOrder = _conditionalOrders.FirstOrDefault(o => o.Id == order.Id);
            if (existingOrder == null)
            {
                return false;
            }

            // 更新属性
            existingOrder.Symbol = order.Symbol;
            existingOrder.Side = order.Side;
            existingOrder.Type = order.Type;
            existingOrder.Quantity = order.Quantity;
            existingOrder.TriggerPrice = order.TriggerPrice;
            existingOrder.OrderPrice = order.OrderPrice;
            existingOrder.Remark = order.Remark;

            await SaveConditionalOrdersAsync();
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> DeleteConditionalOrderAsync(long id)
    {
        try
        {
            var order = _conditionalOrders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return false;
            }

            if (order.IsActive)
            {
                return false;
            }

            _conditionalOrders.Remove(order);
            await SaveConditionalOrdersAsync();
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> CancelConditionalOrderAsync(long id)
    {
        try
        {
            var order = _conditionalOrders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return false;
            }

            if (!order.IsActive)
            {
                return false;
            }

            order.Status = "CANCELLED";
            await SaveConditionalOrdersAsync();
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> CheckAndExecuteConditionalOrdersAsync()
    {
        try
        {
            var activeOrders = _conditionalOrders.Where(o => o.IsActive).ToList();
            var executedCount = 0;

            foreach (var order in activeOrders)
            {
                if (await ShouldExecuteConditionalOrderAsync(order))
                {
                    if (await ExecuteConditionalOrderAsync(order))
                    {
                        executedCount++;
                    }
                }
            }

            if (executedCount > 0)
            {
                return true;
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> ExecuteConditionalOrderAsync(ConditionalOrder order)
    {
        try
        {
            order.Status = "TRIGGERED";
            order.TriggerTime = DateTime.Now;

            // 创建交易请求
            var tradingRequest = new TradingRequest
            {
                Symbol = order.Symbol,
                Side = order.Side,
                Type = order.Type == "STOP_LIMIT" ? "LIMIT" : "MARKET",
                Quantity = order.Quantity,
                Price = order.OrderPrice,
                StopPrice = order.TriggerPrice,
                ReduceOnly = order.Type == "STOP_LOSS" || order.Type == "TAKE_PROFIT",
                WorkingType = "CONTRACT_PRICE"
            };

            // 发送订单到币安
            var success = await _binanceService.PlaceOrderAsync(tradingRequest);
            
            if (success)
            {
                order.Status = "EXECUTED";
                order.ExecuteTime = DateTime.Now;
                order.OrderId = DateTime.Now.Ticks.ToString(); // 临时ID
                
                await SaveConditionalOrdersAsync();
                return true;
            }
            else
            {
                order.Status = "FAILED";
                await SaveConditionalOrdersAsync();
                return false;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<List<ConditionalOrder>> GetActiveConditionalOrdersAsync()
    {
        return await Task.FromResult(_conditionalOrders.Where(o => o.IsActive).ToList());
    }

    public async Task<List<ConditionalOrder>> GetConditionalOrdersBySymbolAsync(string symbol)
    {
        return await Task.FromResult(_conditionalOrders.Where(o => o.Symbol == symbol).ToList());
    }

    public async Task<List<ConditionalOrder>> GetConditionalOrdersByStatusAsync(string status)
    {
        return await Task.FromResult(_conditionalOrders.Where(o => o.Status == status).ToList());
    }

    public async Task<int> GetActiveConditionalOrderCountAsync()
    {
        return await Task.FromResult(_conditionalOrders.Count(o => o.IsActive));
    }

    public async Task<decimal> GetTotalConditionalOrderValueAsync()
    {
        var activeOrders = _conditionalOrders.Where(o => o.IsActive);
        var totalValue = activeOrders.Sum(o => o.Quantity * o.TriggerPrice);
        return await Task.FromResult(totalValue);
    }

    private async Task<bool> ShouldExecuteConditionalOrderAsync(ConditionalOrder order)
    {
        try
        {
            var currentPrice = await _binanceService.GetLatestPriceAsync(order.Symbol);
            if (currentPrice <= 0) return false;

            return order.Type switch
            {
                "STOP_LOSS" => order.Side == "SELL" ? currentPrice <= order.TriggerPrice : currentPrice >= order.TriggerPrice,
                "TAKE_PROFIT" => order.Side == "SELL" ? currentPrice >= order.TriggerPrice : currentPrice <= order.TriggerPrice,
                "STOP_MARKET" => order.Side == "SELL" ? currentPrice <= order.TriggerPrice : currentPrice >= order.TriggerPrice,
                "STOP_LIMIT" => order.Side == "SELL" ? currentPrice <= order.TriggerPrice : currentPrice >= order.TriggerPrice,
                _ => false
            };
        }
        catch (Exception)
        {
            return false;
        }
    }

    private List<ConditionalOrder> LoadConditionalOrders()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                return new List<ConditionalOrder>();
            }

            var json = File.ReadAllText(_configPath);
            var orders = JsonSerializer.Deserialize<List<ConditionalOrder>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return orders ?? new List<ConditionalOrder>();
        }
        catch (Exception)
        {
            return new List<ConditionalOrder>();
        }
    }

    private async Task SaveConditionalOrdersAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_conditionalOrders, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(_configPath, json);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 启动条件单监控定时器
    /// </summary>
    public void StartConditionalOrderMonitor()
    {
        if (_disposed) return;
        
        // 启动定时器，每10秒检查一次条件单
        _monitorTimer = new Timer(async _ => await SafeCheckConditionalOrdersAsync(), 
            null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        
        Console.WriteLine("条件单监控器已启动");
    }

    /// <summary>
    /// 安全的条件单检查（避免并发执行）
    /// </summary>
    private async Task SafeCheckConditionalOrdersAsync()
    {
        if (_disposed || !_executionSemaphore.Wait(0))
        {
            return; // 如果已释放或上次检查还在进行中，则跳过
        }

        try
        {
            await CheckAndExecuteConditionalOrdersAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"条件单检查异常: {ex.Message}");
        }
        finally
        {
            _executionSemaphore.Release();
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        try
        {
            // 停止定时器
            _monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _monitorTimer?.Dispose();
            
            // 释放信号量
            _executionSemaphore?.Dispose();
            
            Console.WriteLine("ConditionalOrderService资源已释放");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ConditionalOrderService释放资源异常: {ex.Message}");
        }
    }
}
