using OrderWatch.Models;
using System.Windows;

namespace OrderWatch.Views;

public partial class AccountConfigWindow : Window
{
    public AccountInfo AccountInfo { get; private set; }

    public AccountConfigWindow(AccountInfo? accountInfo = null)
    {
        InitializeComponent();
        AccountInfo = accountInfo ?? new AccountInfo();
        DataContext = this;

        if (accountInfo != null)
        {
            SecretKeyPasswordBox.Password = accountInfo.SecretKey;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AccountNameTextBox.Text) ||
            string.IsNullOrWhiteSpace(ApiKeyTextBox.Text) ||
            string.IsNullOrWhiteSpace(SecretKeyPasswordBox.Password))
        {
            MessageBox.Show("账户名称、API Key 和 Secret Key 不能为空。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AccountInfo.Name = AccountNameTextBox.Text;
        AccountInfo.ApiKey = ApiKeyTextBox.Text;
        AccountInfo.SecretKey = SecretKeyPasswordBox.Password;
        AccountInfo.IsTestNet = IsTestNetCheckBox.IsChecked ?? false;

        if (decimal.TryParse(RiskCapitalTimesTextBox.Text, out var riskTimes) && riskTimes > 0)
        {
            AccountInfo.RiskCapitalTimes = riskTimes;
        }
        else
        {
            AccountInfo.RiskCapitalTimes = 1.0m; // Default to 1.0 if invalid
        }

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
