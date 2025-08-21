using System.ComponentModel;

namespace OrderWatch.Models;

public class CandidateSymbol : INotifyPropertyChanged
{
    private string _symbol = string.Empty;
    private decimal _latestPrice;
    private decimal _priceChangePercent;
    private DateTime _lastUpdateTime;
    private bool _isSelected;

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

    public decimal LatestPrice
    {
        get => _latestPrice;
        set
        {
            if (_latestPrice != value)
            {
                _latestPrice = value;
                OnPropertyChanged(nameof(LatestPrice));
                OnPropertyChanged(nameof(LatestPriceDisplay));
            }
        }
    }

    public decimal PriceChangePercent
    {
        get => _priceChangePercent;
        set
        {
            if (_priceChangePercent != value)
            {
                _priceChangePercent = value;
                OnPropertyChanged(nameof(PriceChangePercent));
                OnPropertyChanged(nameof(PriceChangePercentDisplay));
                OnPropertyChanged(nameof(IsPriceUp));
                OnPropertyChanged(nameof(IsPriceDown));
            }
        }
    }

    public DateTime LastUpdateTime
    {
        get => _lastUpdateTime;
        set
        {
            if (_lastUpdateTime != value)
            {
                _lastUpdateTime = value;
                OnPropertyChanged(nameof(LastUpdateTime));
                OnPropertyChanged(nameof(LastUpdateTimeDisplay));
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    // 计算属性
    public string LatestPriceDisplay => LatestPrice.ToString("F4");
    public string PriceChangePercentDisplay => $"{PriceChangePercent:F2}%";
    public bool IsPriceUp => PriceChangePercent > 0;
    public bool IsPriceDown => PriceChangePercent < 0;
    public string LastUpdateTimeDisplay => LastUpdateTime.ToString("HH:mm:ss");

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
