using OrderWatch.Services;
using OrderWatch.ViewModels;
using OrderWatch.Views;
using System.Windows;

namespace OrderWatch;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
                        // 创建服务实例
                var binanceService = new BinanceService();
                var configService = new ConfigService();
                var conditionalOrderService = new ConditionalOrderService(binanceService);
                var symbolInfoService = new SymbolInfoService(binanceService);
                
                // 创建主视图模型
                var mainViewModel = new MainViewModel(binanceService, configService, conditionalOrderService, symbolInfoService);
        
        // 创建并显示主窗口
        var mainWindow = new MainWindow();
        mainWindow.DataContext = mainViewModel;
        mainWindow.Show();

        base.OnStartup(e);
    }
}

