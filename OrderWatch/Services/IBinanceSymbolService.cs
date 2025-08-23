using System.Collections.Generic;
using System.Threading.Tasks;

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
    /// 释放资源
    /// </summary>
    void Dispose();
}
