using System.ComponentModel;

namespace OrderWatch.Models;

/// <summary>
/// 币安永续合约信息
/// </summary>
public class SymbolInfo : INotifyPropertyChanged
{
    private string _symbol = string.Empty;
    private string _status = string.Empty;
    private string _baseAsset = string.Empty;
    private string _quoteAsset = string.Empty;
    private int _quantityPrecision;
    private int _pricePrecision;
    private decimal _minQty;
    private decimal _maxQty;
    private decimal _stepSize;
    private decimal _minPrice;
    private decimal _maxPrice;
    private decimal _tickSize;
    private decimal _minNotional;
    private decimal _maxNotional;
    private bool _isMarginTradingAllowed;
    private bool _isSpotTradingAllowed;
    private DateTime _lastUpdate;

    /// <summary>
    /// 合约符号 (如: BTCUSDT)
    /// </summary>
    public string Symbol
    {
        get => _symbol;
        set
        {
            if (_symbol != value)
            {
                _symbol = value;
                OnPropertyChanged(nameof(Symbol));
            }
        }
    }

    /// <summary>
    /// 交易状态 (TRADING, BREAK, AUCTION_MATCH, HALT)
    /// </summary>
    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(IsActive));
            }
        }
    }

    /// <summary>
    /// 基础资产 (如: BTC)
    /// </summary>
    public string BaseAsset
    {
        get => _baseAsset;
        set
        {
            if (_baseAsset != value)
            {
                _baseAsset = value;
                OnPropertyChanged(nameof(BaseAsset));
            }
        }
    }

    /// <summary>
    /// 计价资产 (如: USDT)
    /// </summary>
    public string QuoteAsset
    {
        get => _quoteAsset;
        set
        {
            if (_quoteAsset != value)
            {
                _quoteAsset = value;
                OnPropertyChanged(nameof(QuoteAsset));
            }
        }
    }

    /// <summary>
    /// 数量精度位数
    /// </summary>
    public int QuantityPrecision
    {
        get => _quantityPrecision;
        set
        {
            if (_quantityPrecision != value)
            {
                _quantityPrecision = value;
                OnPropertyChanged(nameof(QuantityPrecision));
            }
        }
    }

    /// <summary>
    /// 价格精度位数
    /// </summary>
    public int PricePrecision
    {
        get => _pricePrecision;
        set
        {
            if (_pricePrecision != value)
            {
                _pricePrecision = value;
                OnPropertyChanged(nameof(PricePrecision));
            }
        }
    }

    /// <summary>
    /// 最小下单数量
    /// </summary>
    public decimal MinQty
    {
        get => _minQty;
        set
        {
            if (_minQty != value)
            {
                _minQty = value;
                OnPropertyChanged(nameof(MinQty));
            }
        }
    }

    /// <summary>
    /// 最大下单数量
    /// </summary>
    public decimal MaxQty
    {
        get => _maxQty;
        set
        {
            if (_maxQty != value)
            {
                _maxQty = value;
                OnPropertyChanged(nameof(MaxQty));
            }
        }
    }

    /// <summary>
    /// 数量步长
    /// </summary>
    public decimal StepSize
    {
        get => _stepSize;
        set
        {
            if (_stepSize != value)
            {
                _stepSize = value;
                OnPropertyChanged(nameof(StepSize));
            }
        }
    }

    /// <summary>
    /// 最小价格
    /// </summary>
    public decimal MinPrice
    {
        get => _minPrice;
        set
        {
            if (_minPrice != value)
            {
                _minPrice = value;
                OnPropertyChanged(nameof(MinPrice));
            }
        }
    }

    /// <summary>
    /// 最大价格
    /// </summary>
    public decimal MaxPrice
    {
        get => _maxPrice;
        set
        {
            if (_maxPrice != value)
            {
                _maxPrice = value;
                OnPropertyChanged(nameof(MaxPrice));
            }
        }
    }

    /// <summary>
    /// 价格步长
    /// </summary>
    public decimal TickSize
    {
        get => _tickSize;
        set
        {
            if (_tickSize != value)
            {
                _tickSize = value;
                OnPropertyChanged(nameof(TickSize));
            }
        }
    }

    /// <summary>
    /// 最小名义价值
    /// </summary>
    public decimal MinNotional
    {
        get => _minNotional;
        set
        {
            if (_minNotional != value)
            {
                _minNotional = value;
                OnPropertyChanged(nameof(MinNotional));
            }
        }
    }

    /// <summary>
    /// 最大名义价值
    /// </summary>
    public decimal MaxNotional
    {
        get => _maxNotional;
        set
        {
            if (_maxNotional != value)
            {
                _maxNotional = value;
                OnPropertyChanged(nameof(MaxNotional));
            }
        }
    }

    /// <summary>
    /// 是否允许保证金交易
    /// </summary>
    public bool IsMarginTradingAllowed
    {
        get => _isMarginTradingAllowed;
        set
        {
            if (_isMarginTradingAllowed != value)
            {
                _isMarginTradingAllowed = value;
                OnPropertyChanged(nameof(IsMarginTradingAllowed));
            }
        }
    }

    /// <summary>
    /// 是否允许现货交易
    /// </summary>
    public bool IsSpotTradingAllowed
    {
        get => _isSpotTradingAllowed;
        set
        {
            if (_isSpotTradingAllowed != value)
            {
                _isSpotTradingAllowed = value;
                OnPropertyChanged(nameof(IsSpotTradingAllowed));
            }
        }
    }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdate
    {
        get => _lastUpdate;
        set
        {
            if (_lastUpdate != value)
            {
                _lastUpdate = value;
                OnPropertyChanged(nameof(LastUpdate));
            }
        }
    }

    // 计算属性
    /// <summary>
    /// 是否为活跃交易合约
    /// </summary>
    public bool IsActive => Status == "TRADING";

    /// <summary>
    /// 显示用的完整信息
    /// </summary>
    public string DisplayInfo => $"{Symbol} | 价格精度:{PricePrecision} | 数量精度:{QuantityPrecision} | 最小数量:{MinQty}";

    /// <summary>
    /// 根据价格精度格式化价格
    /// </summary>
    public string FormatPrice(decimal price)
    {
        return price.ToString($"F{PricePrecision}");
    }

    /// <summary>
    /// 根据数量精度格式化数量
    /// </summary>
    public string FormatQuantity(decimal quantity)
    {
        return quantity.ToString($"F{QuantityPrecision}");
    }

    /// <summary>
    /// 验证数量是否符合规则
    /// </summary>
    public bool IsValidQuantity(decimal quantity)
    {
        if (quantity < MinQty || quantity > MaxQty)
            return false;

        // 检查是否符合步长要求
        var remainder = (quantity - MinQty) % StepSize;
        return Math.Abs(remainder) < 0.0000000001m; // 浮点精度误差容忍
    }

    /// <summary>
    /// 验证价格是否符合规则
    /// </summary>
    public bool IsValidPrice(decimal price)
    {
        if (price < MinPrice || price > MaxPrice)
            return false;

        // 检查是否符合步长要求
        var remainder = (price - MinPrice) % TickSize;
        return Math.Abs(remainder) < 0.0000000001m; // 浮点精度误差容忍
    }

    /// <summary>
    /// 调整数量到有效值
    /// </summary>
    public decimal AdjustQuantity(decimal quantity)
    {
        if (quantity < MinQty) return MinQty;
        if (quantity > MaxQty) return MaxQty;

        // 调整到最近的有效步长
        var steps = Math.Round((quantity - MinQty) / StepSize, 0);
        return MinQty + steps * StepSize;
    }

    /// <summary>
    /// 调整价格到有效值
    /// </summary>
    public decimal AdjustPrice(decimal price)
    {
        if (price < MinPrice) return MinPrice;
        if (price > MaxPrice) return MaxPrice;

        // 调整到最近的有效步长
        var steps = Math.Round((price - MinPrice) / TickSize, 0);
        return MinPrice + steps * TickSize;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
