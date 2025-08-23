@echo off
echo 正在强制结束OrderWatch进程...

REM 查找并结束所有OrderWatch相关进程
taskkill /f /im "OrderWatch.exe" 2>nul
taskkill /f /im "OrderWatch" 2>nul

REM 查找并结束所有dotnet相关进程（如果使用dotnet run）
taskkill /f /im "dotnet.exe" /fi "WINDOWTITLE eq OrderWatch*" 2>nul

echo 进程清理完成！
echo 现在可以重新编译和运行程序了。
pause
