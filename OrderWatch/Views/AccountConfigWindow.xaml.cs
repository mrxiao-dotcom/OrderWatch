using OrderWatch.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OrderWatch.Services;

namespace OrderWatch.Views;

public partial class AccountConfigWindow : Window
{
    public AccountInfo AccountInfo { get; private set; }
    private readonly bool _isEditMode;

    public AccountConfigWindow(AccountInfo? accountInfo = null)
    {
        InitializeComponent();
        
        _isEditMode = accountInfo != null;
        AccountInfo = accountInfo ?? new AccountInfo
        {
            Name = "",
            ApiKey = "",
            SecretKey = "",
            IsTestNet = true, // 默认测试网
            RiskCapitalTimes = 100.0m // 默认100次
        };
        
        DataContext = this;
        
        // 设置窗口标题
        Title = _isEditMode ? "编辑账户" : "新建账户";
        
        // 如果是编辑模式，填充Secret Key
        if (_isEditMode && !string.IsNullOrEmpty(AccountInfo.SecretKey))
        {
            SecretKeyPasswordBox.Password = AccountInfo.SecretKey;
        }
        
        // 设置焦点
        AccountNameTextBox.Focus();
        
        // 如果是编辑模式，选择账户名称
        if (_isEditMode)
        {
            AccountNameTextBox.SelectAll();
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // 验证账户名称
        if (string.IsNullOrWhiteSpace(AccountNameTextBox.Text))
        {
            MessageBox.Show("请输入账户名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            AccountNameTextBox.Focus();
            return;
        }

        // 验证API Key
        if (string.IsNullOrWhiteSpace(ApiKeyTextBox.Text))
        {
            MessageBox.Show("请输入API Key", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            ApiKeyTextBox.Focus();
            return;
        }

        // 验证Secret Key
        if (string.IsNullOrWhiteSpace(SecretKeyPasswordBox.Password))
        {
            MessageBox.Show("请输入Secret Key", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            SecretKeyPasswordBox.Focus();
            return;
        }

        // 验证风险次数
        if (!decimal.TryParse(RiskCapitalTimesTextBox.Text, out var riskTimes) || riskTimes <= 0)
        {
            MessageBox.Show("风险次数必须是大于0的数字", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            RiskCapitalTimesTextBox.Focus();
            RiskCapitalTimesTextBox.SelectAll();
            return;
        }

        // 基本长度验证
        if (ApiKeyTextBox.Text.Length < 10)
        {
            MessageBox.Show("API Key长度太短，请检查输入", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            ApiKeyTextBox.Focus();
            return;
        }

        if (SecretKeyPasswordBox.Password.Length < 10)
        {
            MessageBox.Show("Secret Key长度太短，请检查输入", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            SecretKeyPasswordBox.Focus();
            return;
        }

        // 更新账户信息
        AccountInfo.Name = AccountNameTextBox.Text.Trim();
        AccountInfo.ApiKey = ApiKeyTextBox.Text.Trim();
        AccountInfo.SecretKey = SecretKeyPasswordBox.Password.Trim();
        AccountInfo.IsTestNet = IsTestNetCheckBox.IsChecked ?? false;
        AccountInfo.RiskCapitalTimes = riskTimes;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
    
    private void RiskCapitalTimesTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // 实时验证风险次数输入
        if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
        {
            if (!decimal.TryParse(textBox.Text, out var value) || value <= 0)
            {
                textBox.Background = System.Windows.Media.Brushes.LightPink;
            }
            else
            {
                textBox.Background = System.Windows.Media.Brushes.White;
            }
        }
    }
    
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 窗口加载完成后设置焦点
        if (_isEditMode)
        {
            AccountNameTextBox.SelectAll();
        }
        AccountNameTextBox.Focus();
    }
    
    private async void TestApiButton_Click(object sender, RoutedEventArgs e)
    {
        // 验证必要字段是否已填写
        if (string.IsNullOrWhiteSpace(ApiKeyTextBox.Text))
        {
            ShowTestResult("❌ 请先输入API Key", Brushes.Red);
            return;
        }

        if (string.IsNullOrWhiteSpace(SecretKeyPasswordBox.Password))
        {
            ShowTestResult("❌ 请先输入Secret Key", Brushes.Red);
            return;
        }

        try
        {
            // 禁用按钮并显示测试中状态
            TestApiButton.IsEnabled = false;
            ShowTestResult("🔄 测试中...", Brushes.Orange);

            // 创建临时的BinanceService进行测试
            var testService = new BinanceService();
            testService.SetCredentials(
                ApiKeyTextBox.Text.Trim(), 
                SecretKeyPasswordBox.Password.Trim(), 
                IsTestNetCheckBox.IsChecked ?? false
            );

            // 测试连接
            bool isConnected = await testService.TestConnectionAsync();
            
            if (isConnected)
            {
                // 尝试获取账户信息以验证API权限
                var accountInfo = await testService.GetDetailedAccountInfoAsync();
                
                if (accountInfo != null)
                {
                    ShowTestResult("✅ API有效，连接成功", Brushes.Green);
                }
                else
                {
                    ShowTestResult("⚠️ 连接成功，但可能缺少权限", Brushes.Orange);
                }
            }
            else
            {
                ShowTestResult("❌ 连接失败，请检查API配置", Brushes.Red);
            }

            // 释放临时服务资源
            testService.Dispose();
        }
        catch (Exception ex)
        {
            ShowTestResult($"❌ 测试失败: {ex.Message}", Brushes.Red);
        }
        finally
        {
            // 重新启用按钮
            TestApiButton.IsEnabled = true;
        }
    }
    
    private void ShowTestResult(string message, Brush color)
    {
        TestResultTextBlock.Text = message;
        TestResultTextBlock.Foreground = color;
    }
}
