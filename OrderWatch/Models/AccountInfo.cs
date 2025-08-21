using System.ComponentModel;

namespace OrderWatch.Models;

public class AccountInfo : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _apiKey = string.Empty;
    private string _secretKey = string.Empty;
    private bool _isTestNet;
    private decimal _riskCapitalTimes = 1.0m;
    private decimal _totalWalletBalance;
    private decimal _totalUnrealizedProfit;
    private decimal _totalMarginBalance;
    private decimal _totalPositionInitialMargin;
    private decimal _totalOpenOrderInitialMargin;
    private decimal _totalInitialMargin;
    private decimal _totalMaintMargin;
    private decimal _totalCrossWalletBalance;
    private decimal _totalCrossUnPnl;
    private decimal _maxWithdrawAmount;

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public string ApiKey
    {
        get => _apiKey;
        set
        {
            if (_apiKey != value)
            {
                _apiKey = value;
                OnPropertyChanged(nameof(ApiKey));
            }
        }
    }

    public string SecretKey
    {
        get => _secretKey;
        set
        {
            if (_secretKey != value)
            {
                _secretKey = value;
                OnPropertyChanged(nameof(SecretKey));
            }
        }
    }

    public bool IsTestNet
    {
        get => _isTestNet;
        set
        {
            if (_isTestNet != value)
            {
                _isTestNet = value;
                OnPropertyChanged(nameof(IsTestNet));
            }
        }
    }

    public decimal RiskCapitalTimes
    {
        get => _riskCapitalTimes;
        set
        {
            if (_riskCapitalTimes != value)
            {
                _riskCapitalTimes = value;
                OnPropertyChanged(nameof(RiskCapitalTimes));
                OnPropertyChanged(nameof(RiskCapital));
            }
        }
    }

    public decimal TotalWalletBalance
    {
        get => _totalWalletBalance;
        set
        {
            if (_totalWalletBalance != value)
            {
                _totalWalletBalance = value;
                OnPropertyChanged(nameof(TotalWalletBalance));
                OnPropertyChanged(nameof(TotalEquity));
            }
        }
    }

    public decimal TotalUnrealizedProfit
    {
        get => _totalUnrealizedProfit;
        set
        {
            if (_totalUnrealizedProfit != value)
            {
                _totalUnrealizedProfit = value;
                OnPropertyChanged(nameof(TotalUnrealizedProfit));
                OnPropertyChanged(nameof(TotalEquity));
            }
        }
    }

    public decimal TotalMarginBalance
    {
        get => _totalMarginBalance;
        set
        {
            if (_totalMarginBalance != value)
            {
                _totalMarginBalance = value;
                OnPropertyChanged(nameof(TotalMarginBalance));
                OnPropertyChanged(nameof(AvailableBalance));
            }
        }
    }

    public decimal TotalPositionInitialMargin
    {
        get => _totalPositionInitialMargin;
        set
        {
            if (_totalPositionInitialMargin != value)
            {
                _totalPositionInitialMargin = value;
                OnPropertyChanged(nameof(TotalPositionInitialMargin));
            }
        }
    }

    public decimal TotalOpenOrderInitialMargin
    {
        get => _totalOpenOrderInitialMargin;
        set
        {
            if (_totalOpenOrderInitialMargin != value)
            {
                _totalOpenOrderInitialMargin = value;
                OnPropertyChanged(nameof(TotalOpenOrderInitialMargin));
            }
        }
    }

    public decimal TotalInitialMargin
    {
        get => _totalInitialMargin;
        set
        {
            if (_totalInitialMargin != value)
            {
                _totalInitialMargin = value;
                OnPropertyChanged(nameof(TotalInitialMargin));
                OnPropertyChanged(nameof(AvailableBalance));
            }
        }
    }

    public decimal TotalMaintMargin
    {
        get => _totalMaintMargin;
        set
        {
            if (_totalMaintMargin != value)
            {
                _totalMaintMargin = value;
                OnPropertyChanged(nameof(TotalMaintMargin));
            }
        }
    }

    public decimal TotalCrossWalletBalance
    {
        get => _totalCrossWalletBalance;
        set
        {
            if (_totalCrossWalletBalance != value)
            {
                _totalCrossWalletBalance = value;
                OnPropertyChanged(nameof(TotalCrossWalletBalance));
            }
        }
    }

    public decimal TotalCrossUnPnl
    {
        get => _totalCrossUnPnl;
        set
        {
            if (_totalCrossUnPnl != value)
            {
                _totalCrossUnPnl = value;
                OnPropertyChanged(nameof(TotalCrossUnPnl));
            }
        }
    }

    public decimal MaxWithdrawAmount
    {
        get => _maxWithdrawAmount;
        set
        {
            if (_maxWithdrawAmount != value)
            {
                _maxWithdrawAmount = value;
                OnPropertyChanged(nameof(MaxWithdrawAmount));
            }
        }
    }

    // 计算属性
    public decimal TotalEquity => TotalWalletBalance + TotalUnrealizedProfit;
    public decimal AvailableBalance => TotalMarginBalance - TotalInitialMargin;
    public decimal RiskCapital => AvailableBalance * RiskCapitalTimes;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
