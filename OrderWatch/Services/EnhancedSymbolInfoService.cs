using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OrderWatch.Models;

namespace OrderWatch.Services;

/// <summary>
/// 增强的合约信息服务 - 提供完整的币安永续合约信息缓存
/// </summary>
public class EnhancedSymbolInfoService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;
    private readonly string _symbolInfoCacheFile;
    private readonly string _priceDataCacheFile;
    private Dictionary<string, SymbolInfo> _symbolInfoCache;
    private Dictionary<string, decimal> _priceCache;
    private DateTime _lastCacheUpdate;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(24);
    private readonly TimeSpan _priceCacheExpiration = TimeSpan.FromMinutes(5);
    private DateTime _lastPriceCacheUpdate;

    public EnhancedSymbolInfoService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        // 设置缓存目录和文件路径
        _cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OrderWatch", "Cache");
        Directory.CreateDirectory(_cacheDirectory);
        
        _symbolInfoCacheFile = Path.Combine(_cacheDirectory, "symbol_info_cache.json");
        _priceDataCacheFile = Path.Combine(_cacheDirectory, "price_data_cache.json");
        
        _symbolInfoCache = new Dictionary<string, SymbolInfo>(StringComparer.OrdinalIgnoreCase);
        _priceCache = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        _lastCacheUpdate = DateTime.MinValue;
        _lastPriceCacheUpdate = DateTime.MinValue;

        // 异步加载缓存
        _ = Task.Run(async () => await LoadCacheAsync());
    }

    /// <summary>
    /// 获取所有活跃的永续合约符号列表
    /// </summary>
    public async Task<List<string>> GetActiveSymbolsAsync()
    {
        await EnsureCacheValidAsync();
        return _symbolInfoCache.Values
            .Where(s => s.IsActive)
            .Select(s => s.Symbol)
            .OrderBy(s => s)
            .ToList();
    }

    /// <summary>
    /// 获取指定合约的详细信息
    /// </summary>
    public async Task<SymbolInfo?> GetSymbolInfoAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return null;

        await EnsureCacheValidAsync();
        _symbolInfoCache.TryGetValue(symbol.ToUpper(), out var symbolInfo);
        return symbolInfo;
    }

    /// <summary>
    /// 获取指定合约的最新价格
    /// </summary>
    public async Task<decimal> GetSymbolPriceAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return 0;

        await EnsurePriceCacheValidAsync();
        _priceCache.TryGetValue(symbol.ToUpper(), out var price);
        return price;
    }

    /// <summary>
    /// 根据输入获取匹配的合约建议（包含详细信息）
    /// </summary>
    public async Task<List<SymbolInfo>> GetSymbolSuggestionsAsync(string input, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<SymbolInfo>();

        await EnsureCacheValidAsync();
        var normalizedInput = input.Trim().ToUpper();

        var suggestions = _symbolInfoCache.Values
            .Where(s => s.IsActive && s.Symbol.Contains(normalizedInput, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.Symbol.IndexOf(normalizedInput, StringComparison.OrdinalIgnoreCase))
            .ThenBy(s => s.Symbol.Length)
            .Take(maxResults)
            .ToList();

        return suggestions;
    }

    /// <summary>
    /// 验证合约是否可交易
    /// </summary>
    public async Task<bool> IsSymbolTradableAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return false;

        var symbolInfo = await GetSymbolInfoAsync(symbol);
        return symbolInfo?.IsActive == true;
    }

    /// <summary>
    /// 获取合约的价格和数量精度信息
    /// </summary>
    public async Task<(int pricePrecision, int quantityPrecision, decimal minQty, decimal tickSize)> GetSymbolPrecisionAsync(string symbol)
    {
        var symbolInfo = await GetSymbolInfoAsync(symbol);
        if (symbolInfo == null)
            return (4, 4, 0.001m, 0.01m); // 默认值

        return (symbolInfo.PricePrecision, symbolInfo.QuantityPrecision, symbolInfo.MinQty, symbolInfo.TickSize);
    }

    /// <summary>
    /// 调整价格到合约允许的精度
    /// </summary>
    public async Task<decimal> AdjustPriceToValidAsync(string symbol, decimal price)
    {
        var symbolInfo = await GetSymbolInfoAsync(symbol);
        return symbolInfo?.AdjustPrice(price) ?? price;
    }

    /// <summary>
    /// 调整数量到合约允许的精度
    /// </summary>
    public async Task<decimal> AdjustQuantityToValidAsync(string symbol, decimal quantity)
    {
        var symbolInfo = await GetSymbolInfoAsync(symbol);
        return symbolInfo?.AdjustQuantity(quantity) ?? quantity;
    }

    /// <summary>
    /// 强制刷新所有缓存
    /// </summary>
    public async Task RefreshAllCacheAsync()
    {
        await RefreshSymbolInfoCacheAsync();
        await RefreshPriceCacheAsync();
    }

    /// <summary>
    /// 确保合约信息缓存有效
    /// </summary>
    private async Task EnsureCacheValidAsync()
    {
        if (!IsCacheValid())
        {
            await RefreshSymbolInfoCacheAsync();
        }
    }

    /// <summary>
    /// 确保价格缓存有效
    /// </summary>
    private async Task EnsurePriceCacheValidAsync()
    {
        if (!IsPriceCacheValid())
        {
            await RefreshPriceCacheAsync();
        }
    }

    /// <summary>
    /// 检查合约信息缓存是否有效
    /// </summary>
    private bool IsCacheValid()
    {
        return _symbolInfoCache.Any() && 
               DateTime.Now - _lastCacheUpdate < _cacheExpiration;
    }

    /// <summary>
    /// 检查价格缓存是否有效
    /// </summary>
    private bool IsPriceCacheValid()
    {
        return _priceCache.Any() && 
               DateTime.Now - _lastPriceCacheUpdate < _priceCacheExpiration;
    }

    /// <summary>
    /// 刷新合约信息缓存
    /// </summary>
    private async Task RefreshSymbolInfoCacheAsync()
    {
        try
        {
            Console.WriteLine("开始刷新合约信息缓存...");
            
            // 从币安API获取交易规则
            var exchangeInfo = await FetchExchangeInfoAsync();
            if (exchangeInfo.Any())
            {
                var newCache = new Dictionary<string, SymbolInfo>(StringComparer.OrdinalIgnoreCase);
                
                foreach (var symbolInfo in exchangeInfo)
                {
                    newCache[symbolInfo.Symbol] = symbolInfo;
                }
                
                _symbolInfoCache = newCache;
                _lastCacheUpdate = DateTime.Now;
                
                await SaveSymbolInfoCacheAsync();
                Console.WriteLine($"合约信息缓存已更新，共 {_symbolInfoCache.Count} 个合约");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"刷新合约信息缓存失败: {ex.Message}");
        }

        // 如果API调用失败，尝试从本地缓存加载
        await LoadSymbolInfoCacheAsync();
        
        // 如果本地缓存也没有，使用默认数据
        if (!_symbolInfoCache.Any())
        {
            _symbolInfoCache = GetDefaultSymbolInfo();
            _lastCacheUpdate = DateTime.Now;
            await SaveSymbolInfoCacheAsync();
            Console.WriteLine("使用默认合约信息");
        }
    }

    /// <summary>
    /// 刷新价格缓存
    /// </summary>
    private async Task RefreshPriceCacheAsync()
    {
        try
        {
            Console.WriteLine("开始刷新价格缓存...");
            
            var priceData = await FetchPriceDataAsync();
            if (priceData.Any())
            {
                _priceCache = priceData;
                _lastPriceCacheUpdate = DateTime.Now;
                
                await SavePriceCacheAsync();
                Console.WriteLine($"价格缓存已更新，共 {_priceCache.Count} 个合约价格");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"刷新价格缓存失败: {ex.Message}");
            // 价格缓存失败不影响合约信息使用
        }
    }

    /// <summary>
    /// 从币安API获取交易规则信息
    /// </summary>
    private async Task<List<SymbolInfo>> FetchExchangeInfoAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("https://fapi.binance.com/fapi/v1/exchangeInfo");
            return ParseExchangeInfo(response);
        }
        catch (Exception)
        {
            return new List<SymbolInfo>();
        }
    }

    /// <summary>
    /// 从币安API获取价格数据
    /// </summary>
    private async Task<Dictionary<string, decimal>> FetchPriceDataAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("https://fapi.binance.com/fapi/v1/ticker/price");
            return ParsePriceData(response);
        }
        catch (Exception)
        {
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 解析币安交易规则信息
    /// </summary>
    private List<SymbolInfo> ParseExchangeInfo(string jsonResponse)
    {
        var symbolInfoList = new List<SymbolInfo>();
        
        try
        {
            using var document = JsonDocument.Parse(jsonResponse);
            var root = document.RootElement;
            
            if (!root.TryGetProperty("symbols", out var symbolsArray))
                return symbolInfoList;

            foreach (var symbolElement in symbolsArray.EnumerateArray())
            {
                try
                {
                    var symbolInfo = new SymbolInfo
                    {
                        Symbol = symbolElement.GetProperty("symbol").GetString() ?? "",
                        Status = symbolElement.GetProperty("status").GetString() ?? "",
                        BaseAsset = symbolElement.GetProperty("baseAsset").GetString() ?? "",
                        QuoteAsset = symbolElement.GetProperty("quoteAsset").GetString() ?? "",
                        PricePrecision = symbolElement.GetProperty("pricePrecision").GetInt32(),
                        QuantityPrecision = symbolElement.GetProperty("quantityPrecision").GetInt32(),
                        LastUpdate = DateTime.Now
                    };

                    // 解析过滤器信息
                    if (symbolElement.TryGetProperty("filters", out var filtersArray))
                    {
                        ParseFilters(symbolInfo, filtersArray);
                    }

                    // 只添加永续合约 (PERPETUAL)
                    if (symbolElement.TryGetProperty("contractType", out var contractType) &&
                        contractType.GetString() == "PERPETUAL" &&
                        symbolInfo.Status == "TRADING")
                    {
                        symbolInfoList.Add(symbolInfo);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"解析合约信息失败: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析交易规则信息失败: {ex.Message}");
        }

        return symbolInfoList;
    }

    /// <summary>
    /// 解析过滤器信息
    /// </summary>
    private void ParseFilters(SymbolInfo symbolInfo, JsonElement filtersArray)
    {
        foreach (var filter in filtersArray.EnumerateArray())
        {
            if (!filter.TryGetProperty("filterType", out var filterType))
                continue;

            var filterTypeString = filterType.GetString();
            
            switch (filterTypeString)
            {
                case "LOT_SIZE":
                    symbolInfo.MinQty = decimal.Parse(filter.GetProperty("minQty").GetString() ?? "0");
                    symbolInfo.MaxQty = decimal.Parse(filter.GetProperty("maxQty").GetString() ?? "0");
                    symbolInfo.StepSize = decimal.Parse(filter.GetProperty("stepSize").GetString() ?? "0");
                    break;
                    
                case "PRICE_FILTER":
                    symbolInfo.MinPrice = decimal.Parse(filter.GetProperty("minPrice").GetString() ?? "0");
                    symbolInfo.MaxPrice = decimal.Parse(filter.GetProperty("maxPrice").GetString() ?? "0");
                    symbolInfo.TickSize = decimal.Parse(filter.GetProperty("tickSize").GetString() ?? "0");
                    break;
                    
                case "MIN_NOTIONAL":
                    symbolInfo.MinNotional = decimal.Parse(filter.GetProperty("notional").GetString() ?? "0");
                    break;
            }
        }
    }

    /// <summary>
    /// 解析价格数据
    /// </summary>
    private Dictionary<string, decimal> ParsePriceData(string jsonResponse)
    {
        var priceData = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        
        try
        {
            using var document = JsonDocument.Parse(jsonResponse);
            
            foreach (var priceElement in document.RootElement.EnumerateArray())
            {
                var symbol = priceElement.GetProperty("symbol").GetString();
                var priceString = priceElement.GetProperty("price").GetString();
                
                if (!string.IsNullOrEmpty(symbol) && !string.IsNullOrEmpty(priceString) &&
                    decimal.TryParse(priceString, out var price))
                {
                    priceData[symbol] = price;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析价格数据失败: {ex.Message}");
        }

        return priceData;
    }

    /// <summary>
    /// 获取默认的合约信息
    /// </summary>
    private Dictionary<string, SymbolInfo> GetDefaultSymbolInfo()
    {
        var defaultSymbols = new Dictionary<string, SymbolInfo>(StringComparer.OrdinalIgnoreCase);
        
        var commonSymbols = new[]
        {
            ("BTCUSDT", 2, 3), ("ETHUSDT", 2, 3), ("BNBUSDT", 2, 2),
            ("ADAUSDT", 4, 0), ("DOTUSDT", 3, 1), ("LINKUSDT", 3, 2),
            ("LTCUSDT", 2, 3), ("BCHUSDT", 2, 3), ("XRPUSDT", 4, 0),
            ("SOLUSDT", 3, 2), ("MATICUSDT", 4, 0), ("AVAXUSDT", 3, 2)
        };

        foreach (var (symbol, pricePrecision, quantityPrecision) in commonSymbols)
        {
            defaultSymbols[symbol] = new SymbolInfo
            {
                Symbol = symbol,
                Status = "TRADING",
                BaseAsset = symbol.Replace("USDT", ""),
                QuoteAsset = "USDT",
                PricePrecision = pricePrecision,
                QuantityPrecision = quantityPrecision,
                MinQty = 0.001m,
                MaxQty = 100000m,
                StepSize = 0.001m,
                MinPrice = 0.01m,
                MaxPrice = 1000000m,
                TickSize = 0.01m,
                MinNotional = 5m,
                LastUpdate = DateTime.Now
            };
        }

        return defaultSymbols;
    }

    /// <summary>
    /// 加载所有缓存
    /// </summary>
    private async Task LoadCacheAsync()
    {
        await Task.WhenAll(
            LoadSymbolInfoCacheAsync(),
            LoadPriceCacheAsync()
        );
    }

    /// <summary>
    /// 加载合约信息缓存
    /// </summary>
    private async Task LoadSymbolInfoCacheAsync()
    {
        try
        {
            if (File.Exists(_symbolInfoCacheFile))
            {
                var json = await File.ReadAllTextAsync(_symbolInfoCacheFile, Encoding.UTF8);
                var cacheData = JsonSerializer.Deserialize<CacheData>(json);
                
                if (cacheData != null && cacheData.SymbolInfos.Any())
                {
                    _symbolInfoCache = cacheData.SymbolInfos.ToDictionary(
                        s => s.Symbol, 
                        s => s, 
                        StringComparer.OrdinalIgnoreCase);
                    _lastCacheUpdate = cacheData.LastUpdate;
                    Console.WriteLine($"已加载合约信息缓存，共 {_symbolInfoCache.Count} 个合约");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载合约信息缓存失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载价格缓存
    /// </summary>
    private async Task LoadPriceCacheAsync()
    {
        try
        {
            if (File.Exists(_priceDataCacheFile))
            {
                var json = await File.ReadAllTextAsync(_priceDataCacheFile, Encoding.UTF8);
                var cacheData = JsonSerializer.Deserialize<PriceCacheData>(json);
                
                if (cacheData != null && cacheData.PriceData.Any())
                {
                    _priceCache = new Dictionary<string, decimal>(cacheData.PriceData, StringComparer.OrdinalIgnoreCase);
                    _lastPriceCacheUpdate = cacheData.LastUpdate;
                    Console.WriteLine($"已加载价格缓存，共 {_priceCache.Count} 个合约价格");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载价格缓存失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 保存合约信息缓存
    /// </summary>
    private async Task SaveSymbolInfoCacheAsync()
    {
        try
        {
            var cacheData = new CacheData
            {
                SymbolInfos = _symbolInfoCache.Values.ToList(),
                LastUpdate = _lastCacheUpdate
            };
            
            var json = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            await File.WriteAllTextAsync(_symbolInfoCacheFile, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存合约信息缓存失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 保存价格缓存
    /// </summary>
    private async Task SavePriceCacheAsync()
    {
        try
        {
            var cacheData = new PriceCacheData
            {
                PriceData = _priceCache,
                LastUpdate = _lastPriceCacheUpdate
            };
            
            var json = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            await File.WriteAllTextAsync(_priceDataCacheFile, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存价格缓存失败: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    /// <summary>
    /// 缓存数据结构
    /// </summary>
    private class CacheData
    {
        public List<SymbolInfo> SymbolInfos { get; set; } = new();
        public DateTime LastUpdate { get; set; }
    }

    /// <summary>
    /// 价格缓存数据结构
    /// </summary>
    private class PriceCacheData
    {
        public Dictionary<string, decimal> PriceData { get; set; } = new();
        public DateTime LastUpdate { get; set; }
    }
} 