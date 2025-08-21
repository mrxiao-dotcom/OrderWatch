using OrderWatch.Models;
using System.Windows;

namespace OrderWatch.Views;

public partial class AccountConfigWindow : Window
{
    public AccountInfo AccountInfo { get; private set; }

    public AccountConfigWindow(AccountInfo? accountInfo = null)
    {
        InitializeComponent();
        
        if (accountInfo != null)
        {
            AccountInfo = accountInfo;
            DataContext = AccountInfo;
            
            // 设置Secret Key
            if (!string.IsNullOrEmpty(AccountInfo.SecretKey))
            {
                SecretKeyBox.Password = AccountInfo.SecretKey;
            }
        }
        else
        {
            AccountInfo = new AccountInfo();
            DataContext = AccountInfo;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // 验证必填字段
        if (string.IsNullOrWhiteSpace(AccountInfo.Name))
        {
            MessageBox.Show("请输入账户名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(AccountInfo.ApiKey))
        {
            MessageBox.Show("请输入API Key", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(SecretKeyBox.Password))
        {
            MessageBox.Show("请输入Secret Key", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 更新Secret Key
        AccountInfo.SecretKey = SecretKeyBox.Password;

        // 设置默认值
        if (AccountInfo.RiskCapitalTimes <= 0)
        {
            AccountInfo.RiskCapitalTimes = 1.0m;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
