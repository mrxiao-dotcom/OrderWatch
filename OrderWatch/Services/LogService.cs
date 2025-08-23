using OrderWatch.Models;
using System.Text.Json;
using System.IO;

namespace OrderWatch.Services;

public class LogService : ILogService
{
    private readonly string _logDirectory;
    private readonly string _logFilePath;
    private readonly string _tradeLogFilePath;
    private readonly object _lockObject = new object();
    
    public LogService()
    {
        _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        _logFilePath = Path.Combine(_logDirectory, "application.log");
        _tradeLogFilePath = Path.Combine(_logDirectory, "trades.log");
        
        // 确保日志目录存在
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }
    
    public async Task LogInfoAsync(string message, string? category = null)
    {
        await WriteLogAsync("INFO", message, category);
    }
    
    public async Task LogWarningAsync(string message, string? category = null)
    {
        await WriteLogAsync("WARNING", message, category);
    }
    
    public async Task LogErrorAsync(string message, Exception? exception = null, string? category = null)
    {
        var exceptionDetails = exception?.ToString();
        await WriteLogAsync("ERROR", message, category, exceptionDetails);
    }
    
    public async Task LogTradeAsync(string symbol, string action, decimal? price = null, decimal? quantity = null, string? result = null)
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = "TRADE",
            Category = "Trading",
            Message = $"{action} {symbol}",
            Symbol = symbol,
            Action = action,
            Price = price,
            Quantity = quantity,
            Result = result
        };
        
        await WriteTradeLogAsync(logEntry);
    }
    
    public async Task<List<LogEntry>> GetLogsAsync(DateTime startTime, DateTime endTime, string? category = null, int maxCount = 1000)
    {
        var logs = new List<LogEntry>();
        
        try
        {
            if (File.Exists(_logFilePath))
            {
                string[] lines;
                using (var fileStream = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fileStream))
                {
                    var content = await reader.ReadToEndAsync();
                    lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                }
                var count = 0;
                
                foreach (var line in lines.Reverse()) // 从最新的开始
                {
                    if (count >= maxCount) break;
                    
                    try
                    {
                        var logEntry = JsonSerializer.Deserialize<LogEntry>(line);
                        if (logEntry != null && 
                            logEntry.Timestamp >= startTime && 
                            logEntry.Timestamp <= endTime &&
                            (string.IsNullOrEmpty(category) || logEntry.Category == category))
                        {
                            logs.Add(logEntry);
                            count++;
                        }
                    }
                    catch
                    {
                        // 忽略无法解析的行
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await LogErrorAsync("获取日志失败", ex, "LogService");
        }
        
        return logs.OrderByDescending(l => l.Timestamp).ToList();
    }
    
    public async Task CleanupExpiredLogsAsync(int daysToKeep = 30)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            
            if (File.Exists(_logFilePath))
            {
                var lines = await File.ReadAllLinesAsync(_logFilePath);
                var validLines = new List<string>();
                
                foreach (var line in lines)
                {
                    try
                    {
                        var logEntry = JsonSerializer.Deserialize<LogEntry>(line);
                        if (logEntry?.Timestamp >= cutoffDate)
                        {
                            validLines.Add(line);
                        }
                    }
                    catch
                    {
                        // 保留无法解析的行
                        validLines.Add(line);
                    }
                }
                
                await File.WriteAllLinesAsync(_logFilePath, validLines);
            }
            
            // 清理交易日志
            if (File.Exists(_tradeLogFilePath))
            {
                var lines = await File.ReadAllLinesAsync(_tradeLogFilePath);
                var validLines = new List<string>();
                
                foreach (var line in lines)
                {
                    try
                    {
                        var logEntry = JsonSerializer.Deserialize<LogEntry>(line);
                        if (logEntry?.Timestamp >= cutoffDate)
                        {
                            validLines.Add(line);
                        }
                    }
                    catch
                    {
                        validLines.Add(line);
                    }
                }
                
                await File.WriteAllLinesAsync(_tradeLogFilePath, validLines);
            }
        }
        catch (Exception ex)
        {
            await LogErrorAsync("清理过期日志失败", ex, "LogService");
        }
    }
    
    private async Task WriteLogAsync(string level, string message, string? category = null, string? exceptionDetails = null)
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Category = category ?? "General",
            Message = message,
            ExceptionDetails = exceptionDetails
        };
        
        var jsonLine = JsonSerializer.Serialize(logEntry) + Environment.NewLine;
        
        try
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    // 使用 FileStream 以共享方式写入，避免文件锁定
                    using var fileStream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(fileStream);
                    writer.Write(jsonLine);
                    writer.Flush();
                }
            });
            
            // 同时输出到控制台
            Console.WriteLine($"[{logEntry.FormattedTimestamp}] {logEntry.FormattedMessage}");
        }
        catch (Exception ex)
        {
            // 如果写入日志失败，至少输出到控制台
            Console.WriteLine($"日志写入失败: {ex.Message}");
            Console.WriteLine($"[{logEntry.FormattedTimestamp}] {logEntry.FormattedMessage}");
        }
    }
    
    private async Task WriteTradeLogAsync(LogEntry logEntry)
    {
        var jsonLine = JsonSerializer.Serialize(logEntry) + Environment.NewLine;
        
        try
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    // 使用 FileStream 以共享方式写入，避免文件锁定
                    using var fileStream = new FileStream(_tradeLogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(fileStream);
                    writer.Write(jsonLine);
                    writer.Flush();
                }
            });
        }
        catch (Exception ex)
        {
            await LogErrorAsync("写入交易日志失败", ex, "LogService");
        }
    }
}
