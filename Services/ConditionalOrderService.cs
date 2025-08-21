using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrderWatch.Models;

namespace OrderWatch.Services;

public class ConditionalOrderService : IConditionalOrderService
{
    private readonly ILogger<ConditionalOrderService> _logger;
    private readonly IBinanceService _binanceService;
    private readonly string _configPath;
    private readonly List<ConditionalOrder> _conditionalOrders;
    private long _nextId = 1;

    public ConditionalOrderService(ILogger<ConditionalOrderService> logger, IBinanceService binanceService)
    {
        _logger = logger;
        _binanceService = binanceService;
        _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OrderWatch", "conditional_orders.json");
        _conditionalOrders = LoadConditionalOrders();
        
        // 启动定时器检查条件单
        StartConditionalOrderMonitor();
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
            
            _logger.LogInformation("创建条件单成功: {Id} {Symbol} {Side}", order.Id, order.Symbol, order.Side);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建条件单失败");
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
                _logger.LogWarning("条件单不存在: {Id}", order.Id);
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
            
            _logger.LogInformation("更新条件单成功: {Id}", order.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新条件单失败: {Id}", order.Id);
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
                _logger.LogWarning("条件单不存在: {Id}", id);
                return false;
            }

            if (order.Status == "PENDING" || order.Status == "TRIGGERED")
            {
                _logger.LogWarning("无法删除活动中的条件单: {Id}", id);
                return false;
            }

            _conditionalOrders.Remove(order);
            await SaveConditionalOrdersAsync();
            
            _logger.LogInformation("删除条件单成功: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除条件单失败: {Id}", id);
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
                _logger.LogWarning("条件单不存在: {Id}", id);
                return false;
            }

            if (order.Status != "PENDING" && order.Status != "TRIGGERED")
            {
                _logger.LogWarning("条件单状态不允许取消: {Id} {Status}", id, order.Status);
                return false;
            }

            order.Status = "CANCELLED";
            order.TriggerTime = DateTime.Now;
            await SaveConditionalOrdersAsync();
            
            _logger.LogInformation("取消条件单成功: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消条件单失败: {Id}", id);
            return false;
        }
    }

    public async Task<bool> CheckAndExecuteConditionalOrdersAsync()
    {
        try
        {
            var activeOrders = _conditionalOrders.Where(o => o.Status == "PENDING" || o.Status == "TRIGGERED").ToList();
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
                _logger.LogInformation("检查条件单完成，执行了 {Count} 个条件单", executedCount);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查条件单失败");
            return false;
        }
    }

    public async Task<bool> ExecuteConditionalOrderAsync(ConditionalOrder order)
    {
        try
        {
            // 创建交易请求
            var tradingRequest = new TradingRequest
            {
                Symbol = order.Symbol,
                Side = order.Side,
                Type = order.Type,
                Quantity = order.Quantity,
                Price = order.OrderPrice,
                TimeInForce = "GTC"
            };

            // 发送订单到币安
            var success = await _binanceService.PlaceOrderAsync(tradingRequest);
            if (success)
            {
                order.Status = "EXECUTED";
                order.ExecuteTime = DateTime.Now;
                await SaveConditionalOrdersAsync();
                
                _logger.LogInformation("条件单执行成功: {Id} {Symbol}", order.Id, order.Symbol);
                return true;
            }
            else
            {
                _logger.LogError("条件单执行失败: {Id} {Symbol}", order.Id, order.Symbol);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行条件单异常: {Id}", order.Id);
            return false;
        }
    }

    public async Task<List<ConditionalOrder>> GetActiveConditionalOrdersAsync()
    {
        return await Task.FromResult(_conditionalOrders.Where(o => o.Status == "PENDING" || o.Status == "TRIGGERED").ToList());
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
        return await Task.FromResult(_conditionalOrders.Count(o => o.Status == "PENDING" || o.Status == "TRIGGERED"));
    }

    public async Task<decimal> GetTotalConditionalOrderValueAsync()
    {
        var activeOrders = _conditionalOrders.Where(o => o.Status == "PENDING" || o.Status == "TRIGGERED");
        var totalValue = activeOrders.Sum(o => o.Quantity * o.OrderPrice);
        return await Task.FromResult(totalValue);
    }

    private async Task<bool> ShouldExecuteConditionalOrderAsync(ConditionalOrder order)
    {
        try
        {
            if (order.Status != "PENDING" && order.Status != "TRIGGERED")
                return false;

            // 获取最新价格
            var latestPrice = await _binanceService.GetLatestPriceAsync(order.Symbol);
            if (latestPrice == 0) return false;

            // 检查触发条件
            bool shouldTrigger = false;
            switch (order.Type)
            {
                case "STOP_MARKET":
                case "STOP_LIMIT":
                    // 止损：价格下跌到触发价时执行
                    if (order.Side == "SELL" && latestPrice <= order.TriggerPrice)
                        shouldTrigger = true;
                    else if (order.Side == "BUY" && latestPrice >= order.TriggerPrice)
                        shouldTrigger = true;
                    break;

                case "TAKE_PROFIT_MARKET":
                case "TAKE_PROFIT_LIMIT":
                    // 止盈：价格上涨到触发价时执行
                    if (order.Side == "SELL" && latestPrice >= order.TriggerPrice)
                        shouldTrigger = true;
                    else if (order.Side == "BUY" && latestPrice <= order.TriggerPrice)
                        shouldTrigger = true;
                    break;

                case "LIMIT":
                    // 限价单：价格达到触发价时执行
                    if (order.Side == "SELL" && latestPrice >= order.TriggerPrice)
                        shouldTrigger = true;
                    else if (order.Side == "BUY" && latestPrice <= order.TriggerPrice)
                        shouldTrigger = true;
                    break;
            }

            if (shouldTrigger)
            {
                order.Status = "TRIGGERED";
                order.TriggerTime = DateTime.Now;
                await SaveConditionalOrdersAsync();
                
                _logger.LogInformation("条件单触发: {Id} {Symbol} 价格: {Price} 触发价: {TriggerPrice}", 
                    order.Id, order.Symbol, latestPrice, order.TriggerPrice);
            }

            return shouldTrigger;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查条件单触发条件失败: {Id}", order.Id);
            return false;
        }
    }

    private List<ConditionalOrder> LoadConditionalOrders()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                _logger.LogInformation("条件单配置文件不存在，创建空列表");
                return new List<ConditionalOrder>();
            }

            var json = File.ReadAllText(_configPath);
            var orders = JsonSerializer.Deserialize<List<ConditionalOrder>>(json) ?? new List<ConditionalOrder>();
            
            // 更新下一个ID
            if (orders.Any())
            {
                _nextId = orders.Max(o => o.Id) + 1;
            }
            
            _logger.LogInformation("成功加载 {Count} 个条件单", orders.Count);
            return orders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载条件单配置失败");
            return new List<ConditionalOrder>();
        }
    }

    private async Task SaveConditionalOrdersAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_conditionalOrders, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_configPath, json);
            _logger.LogDebug("条件单配置已保存到: {Path}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存条件单配置失败");
            throw;
        }
    }

    private void StartConditionalOrderMonitor()
    {
        // 启动定时器，每10秒检查一次条件单
        var timer = new Timer(async _ => await CheckAndExecuteConditionalOrdersAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        _logger.LogInformation("条件单监控器已启动");
    }
}
