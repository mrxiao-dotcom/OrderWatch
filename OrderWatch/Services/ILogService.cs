using OrderWatch.Models;

namespace OrderWatch.Services;

public interface ILogService
{
    /// <summary>
    /// 记录信息日志
    /// </summary>
    Task LogInfoAsync(string message, string? category = null);
    
    /// <summary>
    /// 记录警告日志
    /// </summary>
    Task LogWarningAsync(string message, string? category = null);
    
    /// <summary>
    /// 记录错误日志
    /// </summary>
    Task LogErrorAsync(string message, Exception? exception = null, string? category = null);
    
    /// <summary>
    /// 记录交易操作日志
    /// </summary>
    Task LogTradeAsync(string symbol, string action, decimal? price = null, decimal? quantity = null, string? result = null);
    
    /// <summary>
    /// 获取指定时间范围的日志
    /// </summary>
    Task<List<LogEntry>> GetLogsAsync(DateTime startTime, DateTime endTime, string? category = null, int maxCount = 1000);
    
    /// <summary>
    /// 清理过期日志
    /// </summary>
    Task CleanupExpiredLogsAsync(int daysToKeep = 30);
}
