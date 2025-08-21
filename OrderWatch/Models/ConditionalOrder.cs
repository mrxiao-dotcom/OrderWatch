using System.ComponentModel;

namespace OrderWatch.Models;

public class ConditionalOrder : INotifyPropertyChanged
{
    private long _id;
    private string _symbol = string.Empty;
    private string _side = string.Empty;
    private string _type = string.Empty;
    private decimal _quantity;
    private decimal _triggerPrice;
    private decimal _orderPrice;
    private string _status = "PENDING";
    private DateTime _createTime;
    private DateTime? _triggerTime;
    private DateTime? _executeTime;
    private string _orderId = string.Empty;
    private string _remark = string.Empty;

    public long Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
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

    public decimal TriggerPrice
    {
        get => _triggerPrice;
        set
        {
            if (_triggerPrice != value)
            {
                _triggerPrice = value;
                OnPropertyChanged(nameof(TriggerPrice));
            }
        }
    }

    public decimal OrderPrice
    {
        get => _orderPrice;
        set
        {
            if (_orderPrice != value)
            {
                _orderPrice = value;
                OnPropertyChanged(nameof(OrderPrice));
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

    public DateTime CreateTime
    {
        get => _createTime;
        set
        {
            if (_createTime != value)
            {
                _createTime = value;
                OnPropertyChanged(nameof(CreateTime));
                OnPropertyChanged(nameof(CreateTimeDisplay));
            }
        }
    }

    public DateTime? TriggerTime
    {
        get => _triggerTime;
        set
        {
            if (_triggerTime != value)
            {
                _triggerTime = value;
                OnPropertyChanged(nameof(TriggerTime));
                OnPropertyChanged(nameof(TriggerTimeDisplay));
            }
        }
    }

    public DateTime? ExecuteTime
    {
        get => _executeTime;
        set
        {
            if (_executeTime != value)
            {
                _executeTime = value;
                OnPropertyChanged(nameof(ExecuteTime));
                OnPropertyChanged(nameof(ExecuteTimeDisplay));
            }
        }
    }

    public string OrderId
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

    public string Remark
    {
        get => _remark;
        set
        {
            if (_remark != value)
            {
                _remark = value;
                OnPropertyChanged(nameof(Remark));
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
        "STOP_LOSS" => "止损",
        "TAKE_PROFIT" => "止盈",
        "TRAILING_STOP" => "追踪止损",
        "STOP_MARKET" => "止损市价",
        "STOP_LIMIT" => "止损限价",
        _ => Type
    };

    public string StatusDisplay => Status switch
    {
        "PENDING" => "等待中",
        "TRIGGERED" => "已触发",
        "EXECUTED" => "已执行",
        "CANCELLED" => "已取消",
        "FAILED" => "执行失败",
        _ => Status
    };

    public bool IsActive => Status is "PENDING" or "TRIGGERED";

    public string CreateTimeDisplay => CreateTime.ToString("yyyy-MM-dd HH:mm:ss");
    public string TriggerTimeDisplay => TriggerTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
    public string ExecuteTimeDisplay => ExecuteTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
