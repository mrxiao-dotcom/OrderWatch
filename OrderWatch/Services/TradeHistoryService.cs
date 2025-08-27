using OrderWatch.Models;
using System.Text.Json;
using System.Text;
using System.IO;

namespace OrderWatch.Services;

public class TradeHistoryService : ITradeHistoryService
{
    private readonly string _historyDirectory;
    private readonly string _historyFilePath;
    private readonly object _lockObject = new object();
    private long _nextId = 1;

    public TradeHistoryService()
    {
        _historyDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        _historyFilePath = Path.Combine(_historyDirectory, "trade_history.json");
        
        // 确保目录存在
        if (!Directory.Exists(_historyDirectory))
        {
            Directory.CreateDirectory(_historyDirectory);
        }

        // 延迟初始化ID，避免构造函数中的阻塞操作
        _nextId = 1;
    }

    public async Task<bool> AddTradeHistoryAsync(TradeHistory tradeHistory)
    {
        try
        {
            // 确保ID正确初始化
            await EnsureIdInitializedAsync();
            
            tradeHistory.Id = _nextId++;
            tradeHistory.Timestamp = DateTime.Now;

            var histories = await LoadTradeHistoriesAsync();
            histories.Add(tradeHistory);
            
            await SaveTradeHistoriesAsync(histories);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"添加交易历史失败: {ex.Message}");
            return false;
        }
    }

    public async Task<List<TradeHistory>> GetTradeHistoryAsync(DateTime startDate, DateTime endDate, string? symbol = null, string? category = null)
    {
        try
        {
            var histories = await LoadTradeHistoriesAsync();
            
            var filtered = histories.Where(h => 
                h.Timestamp >= startDate && 
                h.Timestamp <= endDate &&
                (string.IsNullOrEmpty(symbol) || h.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(category) || h.Category.Equals(category, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(h => h.Timestamp)
                .ToList();

            return filtered;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取交易历史失败: {ex.Message}");
            return new List<TradeHistory>();
        }
    }

    public async Task<List<TradeHistory>> GetRecentTradeHistoryAsync(int count = 100)
    {
        try
        {
            var histories = await LoadTradeHistoriesAsync();
            return histories.OrderByDescending(h => h.Timestamp).Take(count).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取最近交易历史失败: {ex.Message}");
            return new List<TradeHistory>();
        }
    }

    public async Task<List<TradeHistory>> GetTradeHistoryBySymbolAsync(string symbol, int days = 30)
    {
        try
        {
            var startDate = DateTime.Now.AddDays(-days);
            var histories = await LoadTradeHistoriesAsync();
            
            return histories.Where(h => 
                h.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase) && 
                h.Timestamp >= startDate)
                .OrderByDescending(h => h.Timestamp)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取合约交易历史失败: {ex.Message}");
            return new List<TradeHistory>();
        }
    }

    public async Task CleanupExpiredTradeHistoryAsync(int daysToKeep = 90)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            var histories = await LoadTradeHistoriesAsync();
            
            var validHistories = histories.Where(h => h.Timestamp >= cutoffDate).ToList();
            
            if (validHistories.Count != histories.Count)
            {
                await SaveTradeHistoriesAsync(validHistories);
                Console.WriteLine($"清理过期交易历史完成，保留 {validHistories.Count} 条记录");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清理过期交易历史失败: {ex.Message}");
        }
    }

    public async Task<bool> ExportToCsvAsync(string filePath, DateTime startDate, DateTime endDate, string? symbol = null)
    {
        try
        {
            var histories = await GetTradeHistoryAsync(startDate, endDate, symbol);
            
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("时间,合约,操作,价格,数量,结果,订单ID,订单类型,方向,杠杆,分类,详情");
            
            foreach (var history in histories)
            {
                csvBuilder.AppendLine($"{history.FormattedTimestamp}," +
                                   $"{history.Symbol}," +
                                   $"{history.Action}," +
                                   $"{history.FormattedPrice}," +
                                   $"{history.FormattedQuantity}," +
                                   $"{history.Result}," +
                                   $"{history.OrderId}," +
                                   $"{history.FormattedOrderType}," +
                                   $"{history.FormattedSide}," +
                                   $"{history.FormattedLeverage}," +
                                   $"{history.Category}," +
                                   $"\"{history.Details}\"");
            }
            
            await File.WriteAllTextAsync(filePath, csvBuilder.ToString(), Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"导出CSV失败: {ex.Message}");
            return false;
        }
    }

    private async Task<List<TradeHistory>> LoadTradeHistoriesAsync()
    {
        try
        {
            if (!File.Exists(_historyFilePath))
            {
                Console.WriteLine($"交易历史文件不存在: {_historyFilePath}");
                return new List<TradeHistory>();
            }

            string content;
            using (var fileStream = new FileStream(_historyFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream))
            {
                content = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrEmpty(content))
            {
                Console.WriteLine("交易历史文件为空");
                return new List<TradeHistory>();
            }

            try
            {
                var histories = JsonSerializer.Deserialize<List<TradeHistory>>(content);
                Console.WriteLine($"成功加载 {histories?.Count ?? 0} 条交易历史记录");
                return histories ?? new List<TradeHistory>();
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON反序列化失败: {jsonEx.Message}");
                Console.WriteLine($"文件内容: {content.Substring(0, Math.Min(200, content.Length))}...");
                
                // 如果JSON文件损坏，创建备份并返回空列表
                var backupPath = _historyFilePath + $".backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                File.Copy(_historyFilePath, backupPath);
                Console.WriteLine($"已创建损坏文件的备份: {backupPath}");
                
                return new List<TradeHistory>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载交易历史失败: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            return new List<TradeHistory>();
        }
    }

    private async Task SaveTradeHistoriesAsync(List<TradeHistory> histories)
    {
        try
        {
            var json = JsonSerializer.Serialize(histories, new JsonSerializerOptions { WriteIndented = true });
            
            using var fileStream = new FileStream(_historyFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(fileStream);
            await writer.WriteAsync(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存交易历史失败: {ex.Message}");
        }
    }

    private async Task EnsureIdInitializedAsync()
    {
        // 如果ID已经被正确初始化（大于1），则无需重新计算
        if (_nextId > 1)
            return;

        try
        {
            if (!File.Exists(_historyFilePath))
            {
                _nextId = 1;
                return;
            }

            var histories = await LoadTradeHistoriesAsync().ConfigureAwait(false);
            _nextId = histories.Count > 0 ? histories.Max(h => h.Id) + 1 : 1;
        }
        catch
        {
            _nextId = 1;
        }
    }

    private long GetNextIdFromFile()
    {
        try
        {
            if (!File.Exists(_historyFilePath))
                return 1;

            // 使用 ConfigureAwait(false) 避免死锁
            var histories = LoadTradeHistoriesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            return histories.Count > 0 ? histories.Max(h => h.Id) + 1 : 1;
        }
        catch
        {
            return 1;
        }
    }
}
