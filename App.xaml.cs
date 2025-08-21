using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderWatch.Services;
using OrderWatch.ViewModels;
using OrderWatch.Views;
using System.Windows;

namespace OrderWatch;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // 注册服务
                services.AddSingleton<IBinanceService, BinanceService>();
                services.AddSingleton<IConfigService, ConfigService>();
                
                // 注册视图模型
                services.AddTransient<MainViewModel>();
                
                // 注册视图
                services.AddTransient<MainWindow>();
                
                // 配置日志
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddDebug();
                });
            })
            .Build();

        // 启动主窗口
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}
