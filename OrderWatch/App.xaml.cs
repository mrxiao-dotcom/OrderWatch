using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;

namespace OrderWatch;

public partial class App : Application
{

    protected override void OnStartup(StartupEventArgs e)
    {
        Console.WriteLine("=== 使用简化启动逻辑 ===");
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Console.WriteLine("=== 应用程序正在退出 ===");
        
        try
        {
            // 给定时器和异步操作一些时间完成
            Thread.Sleep(500);
            
            // 执行垃圾回收（但不强制等待）
            GC.Collect();
            
            Console.WriteLine("=== 应用程序退出完成 ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"应用程序退出异常: {ex.Message}");
        }
        finally
        {
            base.OnExit(e);
            // 移除Environment.Exit(0) - 让应用程序自然退出
        }
    }
}

