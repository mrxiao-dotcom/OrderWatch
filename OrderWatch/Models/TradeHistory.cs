using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OrderWatch.Models;

public class TradeHistory : INotifyPropertyChanged
{
    private long _id;
    private string _symbol = string.Empty;
    private string _action = string.Empty;
    private decimal? _price;
    private decimal? _quantity;
    private string _result = string.Empty;
    private DateTime _timestamp;
    private string _orderId = string.Empty;
    private string _orderType = string.Empty;
    private string _side = string.Empty;
    private decimal? _leverage;
    private string _category = string.Empty;
    private string _details = string.Empty;

    public long Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }
    }

    public string Action
    {
        get => _action;
        set
        {
            if (_action != value)
            {
                _action = value;
                OnPropertyChanged();
            }
        }
    }

    public decimal? Price
    {
        get => _price;
        set
        {
            if (_price != value)
            {
                _price = value;
                OnPropertyChanged();
            }
        }
    }

    public decimal? Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = value;
                OnPropertyChanged();
            }
        }
    }

    public string Result
    {
        get => _result;
        set
        {
            if (_result != value)
            {
                _result = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime Timestamp
    {
        get => _timestamp;
        set
        {
            if (_timestamp != value)
            {
                _timestamp = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }
    }

    public string OrderType
    {
        get => _orderType;
        set
        {
            if (_orderType != value)
            {
                _orderType = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }
    }

    public decimal? Leverage
    {
        get => _leverage;
        set
        {
            if (_leverage != value)
            {
                _leverage = value;
                OnPropertyChanged();
            }
        }
    }

    public string Category
    {
        get => _category;
        set
        {
            if (_category != value)
            {
                _category = value;
                OnPropertyChanged();
            }
        }
    }

    public string Details
    {
        get => _details;
        set
        {
            if (_details != value)
            {
                _details = value;
                OnPropertyChanged();
            }
        }
    }

    // 格式化显示属性
    public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    public string FormattedPrice => Price?.ToString("F4") ?? "-";
    public string FormattedQuantity => Quantity?.ToString("F4") ?? "-";
    public string FormattedLeverage => Leverage?.ToString("F0") + "x" ?? "-";
    public string FormattedSide => Side switch
    {
        "BUY" => "买入",
        "SELL" => "卖出",
        "LONG" => "多头",
        "SHORT" => "空头",
        _ => Side
    };
    public string FormattedOrderType => OrderType switch
    {
        "MARKET" => "市价单",
        "LIMIT" => "限价单",
        "STOP_MARKET" => "止损市价单",
        "STOP_LIMIT" => "止损限价单",
        _ => OrderType
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
