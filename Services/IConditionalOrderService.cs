using OrderWatch.Models;

namespace OrderWatch.Services;

public interface IConditionalOrderService
{
    // 条件单管理
    Task<List<ConditionalOrder>> GetConditionalOrdersAsync();
    Task<ConditionalOrder?> GetConditionalOrderByIdAsync(long id);
    Task<bool> CreateConditionalOrderAsync(ConditionalOrder order);
    Task<bool> UpdateConditionalOrderAsync(ConditionalOrder order);
    Task<bool> DeleteConditionalOrderAsync(long id);
    Task<bool> CancelConditionalOrderAsync(long id);
    
    // 条件单监控和执行
    Task<bool> CheckAndExecuteConditionalOrdersAsync();
    Task<bool> ExecuteConditionalOrderAsync(ConditionalOrder order);
    
    // 条件单查询
    Task<List<ConditionalOrder>> GetActiveConditionalOrdersAsync();
    Task<List<ConditionalOrder>> GetConditionalOrdersBySymbolAsync(string symbol);
    Task<List<ConditionalOrder>> GetConditionalOrdersByStatusAsync(string status);
    
    // 条件单统计
    Task<int> GetActiveConditionalOrderCountAsync();
    Task<decimal> GetTotalConditionalOrderValueAsync();
}
