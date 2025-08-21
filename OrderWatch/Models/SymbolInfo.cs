using System;

namespace OrderWatch.Models;

/// <summary>
/// 合约信息模型
/// </summary>
public class SymbolInfo
{
    /// <summary>
    /// 合约名称
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 最新价格
    /// </summary>
    public decimal LatestPrice { get; set; }

    /// <summary>
    /// 24小时价格变化百分比
    /// </summary>
    public decimal PriceChangePercent { get; set; }

    /// <summary>
    /// 24小时最高价
    /// </summary>
    public decimal HighPrice { get; set; }

    /// <summary>
    /// 24小时最低价
    /// </summary>
    public decimal LowPrice { get; set; }

    /// <summary>
    /// 24小时成交量
    /// </summary>
    public decimal Volume { get; set; }

    /// <summary>
    /// 合约精度（小数位数）
    /// </summary>
    public int PricePrecision { get; set; }

    /// <summary>
    /// 数量精度（小数位数）
    /// </summary>
    public int QuantityPrecision { get; set; }

    /// <summary>
    /// 最小下单数量
    /// </summary>
    public decimal MinQuantity { get; set; }

    /// <summary>
    /// 最小下单金额
    /// </summary>
    public decimal MinNotional { get; set; }

    /// <summary>
    /// 合约状态（TRADING, BREAK等）
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 合约类型（PERPETUAL, CURRENT_QUARTER等）
    /// </summary>
    public string ContractType { get; set; } = string.Empty;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdateTime { get; set; }

    /// <summary>
    /// 缓存过期时间（每天重新查询）
    /// </summary>
    public DateTime CacheExpiryTime { get; set; }

    /// <summary>
    /// 检查缓存是否过期
    /// </summary>
    public bool IsCacheExpired => DateTime.Now > CacheExpiryTime;

    /// <summary>
    /// 设置缓存过期时间为明天
    /// </summary>
    public void SetCacheExpiryToTomorrow()
    {
        CacheExpiryTime = DateTime.Today.AddDays(1);
    }
}
