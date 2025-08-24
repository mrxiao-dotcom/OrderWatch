using System.Collections.Generic;
using System.Threading.Tasks;
using OrderWatch.Models;

namespace OrderWatch.Services;

/// <summary>
/// 币安合约服务接口
/// </summary>
public interface IBinanceSymbolService
{
    /// <summary>
    /// 获取所有可交易的合约列表
    /// </summary>
    /// <returns>可交易的合约列表</returns>
    Task<List<string>> GetTradableSymbolsAsync();
    
    /// <summary>
    /// 验证合约名称是否可交易
    /// </summary>
    /// <param name="symbol">合约名称</param>
    /// <returns>是否可交易</returns>
    Task<bool> IsSymbolTradableAsync(string symbol);
    
    /// <summary>
    /// 根据输入获取匹配的合约建议
    /// </summary>
    /// <param name="input">用户输入</param>
    /// <param name="maxResults">最大结果数</param>
    /// <returns>匹配的合约建议列表</returns>
    Task<List<string>> GetSymbolSuggestionsAsync(string input, int maxResults = 10);
    
    /// <summary>
    /// 规范化合约名称
    /// </summary>
    /// <param name="input">用户输入</param>
    /// <returns>规范化后的合约名称</returns>
    Task<string> NormalizeSymbolAsync(string input);
    
    /// <summary>
    /// 刷新合约列表缓存
    /// </summary>
    Task RefreshSymbolsCacheAsync();

    /// <summary>
    /// 获取指定合约的详细信息
    /// </summary>
    /// <param name="symbol">合约名称</param>
    /// <returns>合约详细信息</returns>
    Task<SymbolInfo?> GetSymbolInfoAsync(string symbol);

    /// <summary>
    /// 获取合约的价格和数量精度信息
    /// </summary>
    /// <param name="symbol">合约名称</param>
    /// <returns>价格精度、数量精度、最小数量、价格步长</returns>
    Task<(int pricePrecision, int quantityPrecision, decimal minQty, decimal tickSize)> GetSymbolPrecisionAsync(string symbol);

    /// <summary>
    /// 调整价格到合约允许的精度
    /// </summary>
    /// <param name="symbol">合约名称</param>
    /// <param name="price">原始价格</param>
    /// <returns>调整后的价格</returns>
    Task<decimal> AdjustPriceToValidAsync(string symbol, decimal price);

    /// <summary>
    /// 调整数量到合约允许的精度
    /// </summary>
    /// <param name="symbol">合约名称</param>
    /// <param name="quantity">原始数量</param>
    /// <returns>调整后的数量</returns>
    Task<decimal> AdjustQuantityToValidAsync(string symbol, decimal quantity);
    
    /// <summary>
    /// 释放资源
    /// </summary>
    void Dispose();
}
