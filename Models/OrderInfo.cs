using System.ComponentModel;

namespace OrderWatch.Models;

public class OrderInfo : INotifyPropertyChanged
{
    private long _orderId;
    private string _symbol = string.Empty;
    private string _side = string.Empty;
    private string _type = string.Empty;
    private decimal _quantity;
    private decimal _price;
    private decimal _stopPrice;
    private string _timeInForce = string.Empty;
    private string _status = string.Empty;
    private decimal _executedQty;
    private decimal _cumQuote;
    private decimal _avgPrice;
    private long _updateTime;
    private string _workingType = string.Empty;
    private bool _reduceOnly;
    private bool _closePosition;
    private string _positionSide = string.Empty;
    private string _activatePrice = string.Empty;
    private string _priceRate = string.Empty;
    private string _priceProtect = string.Empty;

    public long OrderId
    {
        get => _orderId;
        set
        {
            if (_orderId != value)
            {
                _orderId = value;
                OnPropertyChanged(nameof(OrderId));
            }
        }
    }

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

    public string Side
    {
        get => _side;
        set
        {
            if (_side != value)
            {
                _side = value;
                OnPropertyChanged(nameof(Side));
                OnPropertyChanged(nameof(SideDisplay));
            }
        }
    }

    public string Type
    {
        get => _type;
        set
        {
            if (_type != value)
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
                OnPropertyChanged(nameof(TypeDisplay));
            }
        }
    }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
            }
        }
    }

    public decimal Price
    {
        get => _price;
        set
        {
            if (_price != value)
            {
                _price = value;
                OnPropertyChanged(nameof(Price));
            }
        }
    }

    public decimal StopPrice
    {
        get => _stopPrice;
        set
        {
            if (_stopPrice != value)
            {
                _stopPrice = value;
                OnPropertyChanged(nameof(StopPrice));
            }
        }
    }

    public string TimeInForce
    {
        get => _timeInForce;
        set
        {
            if (_timeInForce != value)
            {
                _timeInForce = value;
                OnPropertyChanged(nameof(TimeInForce));
            }
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusDisplay));
                OnPropertyChanged(nameof(IsActive));
            }
        }
    }

    public decimal ExecutedQty
    {
        get => _executedQty;
        set
        {
            if (_executedQty != value)
            {
                _executedQty = value;
                OnPropertyChanged(nameof(ExecutedQty));
                OnPropertyChanged(nameof(RemainingQty));
            }
        }
    }

    public decimal CumQuote
    {
        get => _cumQuote;
        set
        {
            if (_cumQuote != value)
            {
                _cumQuote = value;
                OnPropertyChanged(nameof(CumQuote));
            }
        }
    }

    public decimal AvgPrice
    {
        get => _avgPrice;
        set
        {
            if (_avgPrice != value)
            {
                _avgPrice = value;
                OnPropertyChanged(nameof(AvgPrice));
            }
        }
    }

    public long UpdateTime
    {
        get => _updateTime;
        set
        {
            if (_updateTime != value)
            {
                _updateTime = value;
                OnPropertyChanged(nameof(UpdateTime));
                OnPropertyChanged(nameof(UpdateTimeDisplay));
            }
        }
    }

    public string WorkingType
    {
        get => _workingType;
        set
        {
            if (_workingType != value)
            {
                _workingType = value;
                OnPropertyChanged(nameof(WorkingType));
            }
        }
    }

    public bool ReduceOnly
    {
        get => _reduceOnly;
        set
        {
            if (_reduceOnly != value)
            {
                _reduceOnly = value;
                OnPropertyChanged(nameof(ReduceOnly));
            }
        }
    }

    public bool ClosePosition
    {
        get => _closePosition;
        set
        {
            if (_closePosition != value)
            {
                _closePosition = value;
                OnPropertyChanged(nameof(ClosePosition));
            }
        }
    }

    public string PositionSide
    {
        get => _positionSide;
        set
        {
            if (_positionSide != value)
            {
                _positionSide = value;
                OnPropertyChanged(nameof(PositionSide));
            }
        }
    }

    public string ActivatePrice
    {
        get => _activatePrice;
        set
        {
            if (_activatePrice != value)
            {
                _activatePrice = value;
                OnPropertyChanged(nameof(ActivatePrice));
            }
        }
    }

    public string PriceRate
    {
        get => _priceRate;
        set
        {
            if (_priceRate != value)
            {
                _priceRate = value;
                OnPropertyChanged(nameof(PriceRate));
            }
        }
    }

    public string PriceProtect
    {
        get => _priceProtect;
        set
        {
            if (_priceProtect != value)
            {
                _priceProtect = value;
                OnPropertyChanged(nameof(PriceProtect));
            }
        }
    }

    // 计算属性
    public string SideDisplay => Side switch
    {
        "BUY" => "买入",
        "SELL" => "卖出",
        _ => Side
    };

    public string TypeDisplay => Type switch
    {
        "LIMIT" => "限价单",
        "MARKET" => "市价单",
        "STOP" => "止损单",
        "STOP_MARKET" => "止损市价单",
        "TAKE_PROFIT" => "止盈单",
        "TAKE_PROFIT_MARKET" => "止盈市价单",
        "TRAILING_STOP_MARKET" => "跟踪止损单",
        _ => Type
    };

    public string StatusDisplay => Status switch
    {
        "NEW" => "新建",
        "PARTIALLY_FILLED" => "部分成交",
        "FILLED" => "完全成交",
        "CANCELED" => "已取消",
        "PENDING_CANCEL" => "待取消",
        "REJECTED" => "已拒绝",
        "EXPIRED" => "已过期",
        _ => Status
    };

    public bool IsActive => Status is "NEW" or "PARTIALLY_FILLED";
    public decimal RemainingQty => Quantity - ExecutedQty;
    public string UpdateTimeDisplay => DateTimeOffset.FromUnixTimeMilliseconds(UpdateTime).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
