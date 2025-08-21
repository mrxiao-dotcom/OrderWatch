using System.ComponentModel;

namespace OrderWatch.Models;

public class PositionInfo : INotifyPropertyChanged
{
    private string _symbol = string.Empty;
    private string _positionSide = string.Empty;
    private decimal _positionAmt;
    private decimal _entryPrice;
    private decimal _markPrice;
    private decimal _unRealizedProfit;
    private decimal _liquidationPrice;
    private decimal _leverage;
    private decimal _notional;

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

    public string PositionSide
    {
        get => _positionSide;
        set
        {
            if (_positionSide != value)
            {
                _positionSide = value;
                OnPropertyChanged(nameof(PositionSide));
                OnPropertyChanged(nameof(PositionSideDisplay));
            }
        }
    }

    public decimal PositionAmt
    {
        get => _positionAmt;
        set
        {
            if (_positionAmt != value)
            {
                _positionAmt = value;
                OnPropertyChanged(nameof(PositionAmt));
                OnPropertyChanged(nameof(PositionSideDisplay));
                OnPropertyChanged(nameof(IsLong));
                OnPropertyChanged(nameof(IsShort));
            }
        }
    }

    public decimal EntryPrice
    {
        get => _entryPrice;
        set
        {
            if (_entryPrice != value)
            {
                _entryPrice = value;
                OnPropertyChanged(nameof(EntryPrice));
                OnPropertyChanged(nameof(PriceChangePercent));
            }
        }
    }

    public decimal MarkPrice
    {
        get => _markPrice;
        set
        {
            if (_markPrice != value)
            {
                _markPrice = value;
                OnPropertyChanged(nameof(MarkPrice));
                OnPropertyChanged(nameof(PriceChangePercent));
                OnPropertyChanged(nameof(UnRealizedProfitPercent));
            }
        }
    }

    public decimal UnRealizedProfit
    {
        get => _unRealizedProfit;
        set
        {
            if (_unRealizedProfit != value)
            {
                _unRealizedProfit = value;
                OnPropertyChanged(nameof(UnRealizedProfit));
                OnPropertyChanged(nameof(UnRealizedProfitPercent));
            }
        }
    }

    public decimal LiquidationPrice
    {
        get => _liquidationPrice;
        set
        {
            if (_liquidationPrice != value)
            {
                _liquidationPrice = value;
                OnPropertyChanged(nameof(LiquidationPrice));
            }
        }
    }

    public decimal Leverage
    {
        get => _leverage;
        set
        {
            if (_leverage != value)
            {
                _leverage = value;
                OnPropertyChanged(nameof(Leverage));
            }
        }
    }

    public decimal Notional
    {
        get => _notional;
        set
        {
            if (_notional != value)
            {
                _notional = value;
                OnPropertyChanged(nameof(Notional));
                OnPropertyChanged(nameof(UnRealizedProfitPercent));
            }
        }
    }

    // 显示属性
    public string PositionSideDisplay => PositionAmt > 0 ? "LONG" : "SHORT";
    public bool IsLong => PositionAmt > 0;
    public bool IsShort => PositionAmt < 0;
    public decimal PriceChangePercent => EntryPrice > 0 ? (MarkPrice - EntryPrice) / EntryPrice * 100 : 0;
    public decimal UnRealizedProfitPercent => Notional > 0 ? UnRealizedProfit / Notional * 100 : 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
