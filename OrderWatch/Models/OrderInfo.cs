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
    private decimal _executedQty;
    private string _status = string.Empty;
    private long _updateTime;
    private decimal _origQty;
    private bool _reduceOnly;
    private decimal _stopPrice;

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
                OnPropertyChanged(nameof(RemainingQty));
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

    // 显示属性
    public string SideDisplay => Side switch
    {
        "BUY" => "买入",
        "SELL" => "卖出",
        _ => Side
    };

    public string TypeDisplay => Type switch
    {
        "MARKET" => "市价",
        "LIMIT" => "限价",
        "STOP_MARKET" => "止损市价",
        "STOP_LIMIT" => "止损限价",
        _ => Type
    };

    public string StatusDisplay => Status switch
    {
        "NEW" => "新建",
        "PARTIALLY_FILLED" => "部分成交",
        "FILLED" => "完全成交",
        "CANCELED" => "已取消",
        "REJECTED" => "拒绝",
        "EXPIRED" => "过期",
        _ => Status
    };

    public bool IsActive => Status is "NEW" or "PARTIALLY_FILLED";
    public decimal RemainingQty => OrigQty - ExecutedQty;
    public string UpdateTimeDisplay => DateTimeOffset.FromUnixTimeMilliseconds(UpdateTime).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");

    public decimal OrigQty
    {
        get => _origQty;
        set
        {
            if (_origQty != value)
            {
                _origQty = value;
                OnPropertyChanged(nameof(OrigQty));
                OnPropertyChanged(nameof(RemainingQty));
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
                OnPropertyChanged(nameof(ReduceOnlyDisplay));
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

    public string ReduceOnlyDisplay => ReduceOnly ? "✓" : "";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
