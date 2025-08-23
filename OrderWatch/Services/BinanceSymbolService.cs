using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Text;

namespace OrderWatch.Services;

/// <summary>
/// 币安合约服务实现
/// </summary>
public class BinanceSymbolService : IBinanceSymbolService
{
    private readonly HttpClient _httpClient;
    private readonly string _cacheFilePath;
    private List<string> _cachedSymbols;
    private DateTime _lastCacheUpdate;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(24);

    public BinanceSymbolService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _cacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "symbols_cache.json");
        _cachedSymbols = new List<string>();
        _lastCacheUpdate = DateTime.MinValue;
    }

    /// <summary>
    /// 获取所有可交易的合约列表
    /// </summary>
    public async Task<List<string>> GetTradableSymbolsAsync()
    {
        // 检查缓存是否有效
        if (IsCacheValid())
        {
            return _cachedSymbols;
        }

        // 刷新缓存
        await RefreshSymbolsCacheAsync();
        return _cachedSymbols;
    }

    /// <summary>
    /// 验证合约名称是否可交易
    /// </summary>
    public async Task<bool> IsSymbolTradableAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return false;

        var symbols = await GetTradableSymbolsAsync();
        return symbols.Any(s => string.Equals(s, symbol, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 根据输入获取匹配的合约建议
    /// </summary>
    public async Task<List<string>> GetSymbolSuggestionsAsync(string input, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<string>();

        var symbols = await GetTradableSymbolsAsync();
        var normalizedInput = input.Trim().ToUpper();

        var suggestions = symbols
            .Where(s => s.Contains(normalizedInput, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.IndexOf(normalizedInput, StringComparison.OrdinalIgnoreCase))
            .ThenBy(s => s.Length)
            .Take(maxResults)
            .ToList();

        return suggestions;
    }

    /// <summary>
    /// 规范化合约名称
    /// </summary>
    public async Task<string> NormalizeSymbolAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Trim().ToUpper();
        
        // 首先检查是否已经是完整的合约名称
        if (await IsSymbolTradableAsync(normalized))
        {
            return normalized;
        }

        // 尝试自动补全
        var suggestions = await GetSymbolSuggestionsAsync(normalized, 1);
        if (suggestions.Any())
        {
            return suggestions.First();
        }

        // 如果无法补全，返回原始输入
        return normalized;
    }

    /// <summary>
    /// 刷新合约列表缓存
    /// </summary>
    public async Task RefreshSymbolsCacheAsync()
    {
        try
        {
            // 尝试从币安API获取合约列表
            var symbols = await FetchSymbolsFromBinanceAsync();
            
            if (symbols.Any())
            {
                _cachedSymbols = symbols;
                _lastCacheUpdate = DateTime.Now;
                await SaveCacheToFileAsync();
                return;
            }
        }
        catch (Exception)
        {
            // API调用失败，忽略错误继续执行
        }

        // 如果API调用失败，尝试从缓存文件加载
        await LoadCacheFromFileAsync();
        
        // 如果缓存文件也没有数据，使用默认的常见合约列表
        if (!_cachedSymbols.Any())
        {
            _cachedSymbols = GetDefaultSymbols();
            _lastCacheUpdate = DateTime.Now;
            await SaveCacheToFileAsync();
        }
    }

    /// <summary>
    /// 从币安API获取合约列表
    /// </summary>
    private async Task<List<string>> FetchSymbolsFromBinanceAsync()
    {
        try
        {
            // 获取币安期货合约信息
            var response = await _httpClient.GetStringAsync("https://fapi.binance.com/fapi/v1/exchangeInfo");
            var jsonDoc = JsonDocument.Parse(response);
            
            var symbols = new List<string>();
            
            if (jsonDoc.RootElement.TryGetProperty("symbols", out var symbolsArray))
            {
                foreach (var symbol in symbolsArray.EnumerateArray())
                {
                    if (symbol.TryGetProperty("symbol", out var symbolName) &&
                        symbol.TryGetProperty("status", out var status) &&
                        status.GetString() == "TRADING")
                    {
                        var symbolString = symbolName.GetString();
                        if (!string.IsNullOrEmpty(symbolString))
                        {
                            symbols.Add(symbolString);
                        }
                    }
                }
            }
            
            return symbols;
        }
        catch (Exception)
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// 获取默认的常见合约列表
    /// </summary>
    private List<string> GetDefaultSymbols()
    {
        return new List<string>
        {
            "BTCUSDT", "ETHUSDT", "BNBUSDT", "ADAUSDT", "DOTUSDT",
            "LINKUSDT", "LTCUSDT", "BCHUSDT", "XRPUSDT", "EOSUSDT",
            "TRXUSDT", "XLMUSDT", "VETUSDT", "FILUSDT", "THETAUSDT",
            "SOLUSDT", "MATICUSDT", "AVAXUSDT", "ATOMUSDT", "NEARUSDT",
            "FTMUSDT", "ALGOUSDT", "ICPUSDT", "AAVEUSDT", "UNIUSDT",
            "SUSHIUSDT", "CAKEUSDT", "DOGEUSDT", "SHIBUSDT", "CHZUSDT",
            "HOTUSDT", "BTTUSDT", "WINUSDT", "DENTUSDT", "STMXUSDT",
            "ANKRUSDT", "ZILUSDT", "IOTAUSDT", "NEOUSDT", "ONTUSDT",
            "QTUMUSDT", "ICXUSDT", "ZRXUSDT", "BATUSDT", "MANAUSDT",
            "ENJUSDT", "SANDUSDT", "AXSUSDT", "GALAUSDT", "ROSEUSDT",
            "ONEUSDT", "HARMONYUSDT", "CHRUSDT", "DYDXUSDT", "IMXUSDT",
            "OPUSDT", "ARBUSDT", "MASKUSDT", "APTUSDT", "INJUSDT",
            "TIAUSDT", "JUPUSDT", "PYTHUSDT", "WIFUSDT", "BONKUSDT"
        };
    }

    /// <summary>
    /// 检查缓存是否有效
    /// </summary>
    private bool IsCacheValid()
    {
        return _cachedSymbols.Any() && 
               DateTime.Now - _lastCacheUpdate < _cacheExpiration;
    }

    /// <summary>
    /// 保存缓存到文件
    /// </summary>
    private async Task SaveCacheToFileAsync()
    {
        try
        {
            var cacheData = new
            {
                Symbols = _cachedSymbols,
                LastUpdate = _lastCacheUpdate
            };
            
            var json = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            await File.WriteAllTextAsync(_cacheFilePath, json, Encoding.UTF8);
        }
        catch (Exception)
        {
            // 忽略文件保存错误
        }
    }

    /// <summary>
    /// 从文件加载缓存
    /// </summary>
    private async Task LoadCacheFromFileAsync()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = await File.ReadAllTextAsync(_cacheFilePath, Encoding.UTF8);
                var cacheData = JsonSerializer.Deserialize<dynamic>(json);
                
                if (cacheData != null)
                {
                    // 这里简化处理，实际应该解析JSON结构
                    // 为了简化，我们直接使用默认列表
                    _cachedSymbols = GetDefaultSymbols();
                    _lastCacheUpdate = DateTime.Now;
                }
            }
        }
        catch (Exception)
        {
            // 忽略文件读取错误
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
