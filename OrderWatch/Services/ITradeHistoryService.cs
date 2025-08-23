using OrderWatch.Models;

namespace OrderWatch.Services;

public interface ITradeHistoryService
{
    /// <summary>
    /// 添加交易历史记录
    /// </summary>
    Task<bool> AddTradeHistoryAsync(TradeHistory tradeHistory);

    /// <summary>
    /// 获取指定时间范围内的交易历史
    /// </summary>
    Task<List<TradeHistory>> GetTradeHistoryAsync(DateTime startDate, DateTime endDate, string? symbol = null, string? category = null);

    /// <summary>
    /// 获取最近的交易历史
    /// </summary>
    Task<List<TradeHistory>> GetRecentTradeHistoryAsync(int count = 100);

    /// <summary>
    /// 根据合约名称获取交易历史
    /// </summary>
    Task<List<TradeHistory>> GetTradeHistoryBySymbolAsync(string symbol, int days = 30);

    /// <summary>
    /// 清理过期的交易历史记录
    /// </summary>
    Task CleanupExpiredTradeHistoryAsync(int daysToKeep = 90);

    /// <summary>
    /// 导出交易历史到CSV文件
    /// </summary>
    Task<bool> ExportToCsvAsync(string filePath, DateTime startDate, DateTime endDate, string? symbol = null);
}
