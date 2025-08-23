@echo off
echo 正在强制终止 OrderWatch 进程...

:: 杀死所有 OrderWatch 进程
taskkill /F /IM OrderWatch.exe /T >nul 2>&1

:: 杀死相关的 dotnet 进程（如果通过 dotnet run 启动）
for /f "tokens=2" %%i in ('tasklist /fi "imagename eq dotnet.exe" /fo csv ^| findstr OrderWatch') do (
    taskkill /F /PID %%i >nul 2>&1
)

:: 等待一秒
timeout /t 1 /nobreak >nul

:: 检查是否还有进程
tasklist /fi "imagename eq OrderWatch.exe" 2>nul | findstr /i "OrderWatch.exe" >nul
if %errorlevel% == 0 (
    echo 仍有 OrderWatch 进程运行，尝试更强制的方法...
    wmic process where "name='OrderWatch.exe'" delete >nul 2>&1
) else (
    echo OrderWatch 进程已成功终止
)

:: 清理临时文件
if exist "OrderWatch\bin\Debug\net6.0-windows\*.tmp" del /q "OrderWatch\bin\Debug\net6.0-windows\*.tmp" >nul 2>&1
if exist "OrderWatch\obj\Debug\net6.0-windows\*.tmp" del /q "OrderWatch\obj\Debug\net6.0-windows\*.tmp" >nul 2>&1

echo 清理完成，现在可以重新编译程序了
pause
