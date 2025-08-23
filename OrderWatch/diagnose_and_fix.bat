@echo off
echo ===============================================
echo OrderWatch 诊断和修复脚本
echo ===============================================

echo.
echo 1. 检查 .NET 安装状态...
dotnet --version
echo.
echo 已安装的运行时:
dotnet --list-runtimes
echo.
echo 已安装的SDK:
dotnet --list-sdks

echo.
echo 2. 强制终止残留进程...
taskkill /F /IM "OrderWatch.exe" 2>nul
taskkill /F /IM "dotnet.exe" 2>nul
echo 进程清理完成

echo.
echo 3. 清理项目文件...
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
echo 清理完成

echo.
echo 4. 还原包和重新编译...
dotnet restore
dotnet clean
dotnet build

echo.
echo 5. 检查编译结果...
if exist "bin\Debug\net6.0-windows\OrderWatch.exe" (
    echo ✓ 编译成功!
    echo.
    echo 6. 尝试运行...
    start "" "bin\Debug\net6.0-windows\OrderWatch.exe"
    echo 应用程序已启动，请检查是否有窗口显示
) else (
    echo ✗ 编译失败，请检查错误信息
)

echo.
echo 诊断脚本完成
pause
