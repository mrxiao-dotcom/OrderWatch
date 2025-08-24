using OrderWatch.Models;

namespace OrderWatch.Services;

/// <summary>
/// 合约信息服务接口
/// </summary>
public interface ISymbolInfoService
{
    /// <summary>
    /// 获取合约信息（优先从缓存获取，缓存过期则从API获取）
    /// </summary>
    Task<SymbolInfo?> GetSymbolInfoAsync(string symbol);

    /// <summary>
    /// 刷新合约信息（强制从API获取最新数据）
    /// </summary>
    Task<SymbolInfo?> RefreshSymbolInfoAsync(string symbol);

    /// <summary>
    /// 批量刷新所有缓存的合约信息
    /// </summary>
    Task RefreshAllCachedSymbolsAsync();

    /// <summary>
    /// 清除过期的缓存
    /// </summary>
    Task ClearExpiredCacheAsync();
    
    /// <summary>
    /// 加载缓存的合约信息
    /// </summary>
    Task LoadCachedSymbolsAsync();
}
