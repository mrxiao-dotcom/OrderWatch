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
            // 等待一小段时间确保所有资源释放
            Thread.Sleep(1000);
            
            // 强制垃圾回收
            GC.Collect();
            GC.WaitForPendingFinalizers();
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
            
            // 如果正常退出失败，强制终止进程
            Environment.Exit(0);
        }
    }
}

