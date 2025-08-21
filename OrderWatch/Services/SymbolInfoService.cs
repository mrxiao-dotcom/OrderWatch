using OrderWatch.Models;
using System.Collections.Concurrent;
using System.Text.Json;
using System.IO;

namespace OrderWatch.Services;

/// <summary>
/// 合约信息服务实现
/// </summary>
public class SymbolInfoService : ISymbolInfoService
{
    private readonly IBinanceService _binanceService;
    private readonly string _cacheFilePath;
    private readonly ConcurrentDictionary<string, SymbolInfo> _symbolCache;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public SymbolInfoService(IBinanceService binanceService)
    {
        _binanceService = binanceService;
        _cacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "symbol_cache.json");
        _symbolCache = new ConcurrentDictionary<string, SymbolInfo>();
        
        // 加载缓存的合约信息
        _ = Task.Run(LoadCachedSymbolsAsync);
    }

    /// <summary>
    /// 获取合约信息（优先从缓存获取，缓存过期则从API获取）
    /// </summary>
    public async Task<SymbolInfo?> GetSymbolInfoAsync(string symbol)
    {
        if (string.IsNullOrEmpty(symbol)) return null;

        // 检查缓存
        if (_symbolCache.TryGetValue(symbol, out var cachedInfo) && !cachedInfo.IsCacheExpired)
        {
            return cachedInfo;
        }

        // 缓存过期或不存在，从API获取
        return await RefreshSymbolInfoAsync(symbol);
    }

    /// <summary>
    /// 刷新合约信息（强制从API获取最新数据）
    /// </summary>
    public async Task<SymbolInfo?> RefreshSymbolInfoAsync(string symbol)
    {
        if (string.IsNullOrEmpty(symbol)) return null;

        try
        {
            Console.WriteLine($"正在刷新合约信息: {symbol}");
            
            // 获取最新价格
            var latestPrice = await _binanceService.GetLatestPriceAsync(symbol);
            if (latestPrice <= 0) 
            {
                Console.WriteLine($"获取 {symbol} 最新价格失败");
                return null;
            }

            // 获取24小时价格变化
            var priceChangePercent = await _binanceService.Get24hrPriceChangeAsync(symbol);

            // 创建合约信息对象
            var symbolInfo = new SymbolInfo
            {
                Symbol = symbol,
                LatestPrice = latestPrice,
                PriceChangePercent = priceChangePercent,
                LastUpdateTime = DateTime.Now
            };

            // 设置缓存过期时间为明天
            symbolInfo.SetCacheExpiryToTomorrow();

            // 更新缓存
            _symbolCache.AddOrUpdate(symbol, symbolInfo, (key, oldValue) => symbolInfo);

            // 保存到本地文件
            await SaveCachedSymbolsAsync();

            Console.WriteLine($"成功刷新 {symbol} 信息: 价格={latestPrice}, 24h变化={priceChangePercent}%");
            return symbolInfo;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"刷新 {symbol} 信息异常: {ex.Message}");
            // 静默处理错误，返回null
            return null;
        }
    }

    /// <summary>
    /// 批量刷新所有缓存的合约信息
    /// </summary>
    public async Task RefreshAllCachedSymbolsAsync()
    {
        var symbols = _symbolCache.Keys.ToList();
        var tasks = symbols.Select(symbol => RefreshSymbolInfoAsync(symbol));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 清除过期的缓存
    /// </summary>
    public async Task ClearExpiredCacheAsync()
    {
        var expiredSymbols = _symbolCache.Values
            .Where(info => info.IsCacheExpired)
            .Select(info => info.Symbol)
            .ToList();

        foreach (var symbol in expiredSymbols)
        {
            _symbolCache.TryRemove(symbol, out _);
        }

        if (expiredSymbols.Count > 0)
        {
            await SaveCachedSymbolsAsync();
        }
    }

    /// <summary>
    /// 加载缓存的合约信息
    /// </summary>
    private async Task LoadCachedSymbolsAsync()
    {
        try
        {
            if (!File.Exists(_cacheFilePath)) return;

            var jsonContent = await File.ReadAllTextAsync(_cacheFilePath);
            var cachedSymbols = JsonSerializer.Deserialize<Dictionary<string, SymbolInfo>>(jsonContent);

            if (cachedSymbols != null)
            {
                foreach (var kvp in cachedSymbols)
                {
                    // 只加载未过期的缓存
                    if (!kvp.Value.IsCacheExpired)
                    {
                        _symbolCache.TryAdd(kvp.Key, kvp.Value);
                    }
                }
            }
        }
        catch
        {
            // 静默处理加载失败
        }
    }

    /// <summary>
    /// 保存缓存的合约信息到本地文件
    /// </summary>
    private async Task SaveCachedSymbolsAsync()
    {
        try
        {
            await _cacheLock.WaitAsync();

            var cacheData = _symbolCache.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var jsonContent = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var directory = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(_cacheFilePath, jsonContent);
        }
        finally
        {
            _cacheLock.Release();
        }
    }
}
