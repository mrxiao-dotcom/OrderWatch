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
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            // 给定时器和异步操作一些时间完成
            Thread.Sleep(500);
            
            // 执行垃圾回收（但不强制等待）
            GC.Collect();
        }
        catch (Exception)
        {
            // 静默处理退出异常
        }
        finally
        {
            base.OnExit(e);
        }
    }
}

