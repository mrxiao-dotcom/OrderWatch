# 强制结束OrderWatch进程的PowerShell脚本
Write-Host "正在强制结束OrderWatch进程..." -ForegroundColor Yellow

try {
    # 查找所有OrderWatch相关进程
    $processes = Get-Process | Where-Object { 
        $_.ProcessName -like "*OrderWatch*" -or 
        $_.ProcessName -like "*dotnet*" -and $_.MainWindowTitle -like "*OrderWatch*"
    }
    
    if ($processes.Count -gt 0) {
        Write-Host "找到以下进程:" -ForegroundColor Cyan
        $processes | ForEach-Object {
            Write-Host "  PID: $($_.Id), 名称: $($_.ProcessName), 标题: $($_.MainWindowTitle)" -ForegroundColor White
        }
        
        # 强制结束进程
        $processes | Stop-Process -Force
        Write-Host "所有进程已强制结束" -ForegroundColor Green
    } else {
        Write-Host "未找到OrderWatch相关进程" -ForegroundColor Green
    }
    
    # 等待进程完全结束
    Start-Sleep -Seconds 2
    
    # 再次检查是否还有残留进程
    $remainingProcesses = Get-Process | Where-Object { 
        $_.ProcessName -like "*OrderWatch*" -or 
        $_.ProcessName -like "*dotnet*" -and $_.MainWindowTitle -like "*OrderWatch*"
    }
    
    if ($remainingProcesses.Count -eq 0) {
        Write-Host "进程清理完成！现在可以重新编译和运行程序了。" -ForegroundColor Green
    } else {
        Write-Host "警告：仍有残留进程" -ForegroundColor Red
        $remainingProcesses | ForEach-Object {
            Write-Host "  PID: $($_.Id), 名称: $($_.ProcessName)" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "错误: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "按任意键继续..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
