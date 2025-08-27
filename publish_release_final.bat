@echo off
chcp 65001 >nul
echo.
echo ========================================
echo 🚀 OrderWatch Release 发布脚本 - Final
echo ========================================
echo.

:: 检查是否在正确的目录
if not exist "version.json" (
    echo ❌ 错误：未找到 version.json 文件
    echo 请确保在项目根目录运行此脚本
    pause
    exit /b 1
)

:: 显示当前版本
echo 📊 读取当前版本信息...
for /f "delims=" %%i in ('powershell -Command "(Get-Content 'version.json' | ConvertFrom-Json).Version"') do set CURRENT_VERSION=%%i
echo 当前版本: V%CURRENT_VERSION%

:: 手动递增版本号
echo 🔢 递增版本号 (+0.01)...
.\quick_version.bat

:: 获取新版本
for /f "delims=" %%i in ('powershell -Command "(Get-Content 'version.json' | ConvertFrom-Json).Version"') do set NEW_VERSION=%%i
echo 新版本: V%NEW_VERSION%
echo.

:: 清理之前的发布文件
echo 🧹 清理之前的发布文件...
if exist "release" rd /s /q "release"
if exist "OrderWatch\bin\Release" rd /s /q "OrderWatch\bin\Release"
if exist "OrderWatch\obj\Release" rd /s /q "OrderWatch\obj\Release"

:: 临时禁用版本递增（重命名PowerShell脚本）
echo 🔒 临时禁用自动版本递增...
if exist "OrderWatch\Scripts\IncrementVersion.ps1" (
    ren "OrderWatch\Scripts\IncrementVersion.ps1" "IncrementVersion.ps1.disabled"
)

echo.
echo 🔨 开始构建 Release 版本...
cd OrderWatch

:: 构建（不会触发版本递增，因为脚本被禁用）
dotnet clean --configuration Release --verbosity minimal
dotnet build --configuration Release --verbosity minimal --no-restore

if %ERRORLEVEL% neq 0 (
    echo ❌ Release 构建失败！
    cd ..
    if exist "OrderWatch\Scripts\IncrementVersion.ps1.disabled" (
        ren "OrderWatch\Scripts\IncrementVersion.ps1.disabled" "IncrementVersion.ps1"
    )
    pause
    exit /b 1
)
echo ✅ Release 构建成功！
echo.

:: 发布单文件版本
echo 📦 发布单文件版本...
dotnet publish --configuration Release --output ..\release\single-file --self-contained true --runtime win-x64 --verbosity minimal /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

if %ERRORLEVEL% neq 0 (
    echo ❌ 单文件发布失败！
    cd ..
    if exist "OrderWatch\Scripts\IncrementVersion.ps1.disabled" (
        ren "OrderWatch\Scripts\IncrementVersion.ps1.disabled" "IncrementVersion.ps1"
    )
    pause
    exit /b 1
)
echo ✅ 单文件版本发布成功！
echo.

:: 发布常规版本
echo 📦 发布常规版本...
dotnet publish --configuration Release --output ..\release\portable --self-contained false --runtime win-x64 --verbosity minimal

if %ERRORLEVEL% neq 0 (
    echo ❌ 常规版本发布失败！
    cd ..
    if exist "OrderWatch\Scripts\IncrementVersion.ps1.disabled" (
        ren "OrderWatch\Scripts\IncrementVersion.ps1.disabled" "IncrementVersion.ps1"
    )
    pause
    exit /b 1
)
echo ✅ 常规版本发布成功！
echo.

:: 返回根目录
cd ..

:: 恢复版本递增（重命名回来）
echo 🔓 恢复自动版本递增...
if exist "OrderWatch\Scripts\IncrementVersion.ps1.disabled" (
    ren "OrderWatch\Scripts\IncrementVersion.ps1.disabled" "IncrementVersion.ps1"
)

:: 复制版本文件到发布目录
echo 📄 复制版本文件...
copy /y "version.json" "release\single-file\version.json" >nul
copy /y "version.json" "release\portable\version.json" >nul
echo ✅ 版本文件复制完成！
echo.

:: 创建版本信息文件
echo 📝 创建版本信息文件...
echo OrderWatch - 币安期货下单系统 > "release\VERSION.txt"
echo 版本号: V%NEW_VERSION% >> "release\VERSION.txt"
echo 构建时间: %date% %time% >> "release\VERSION.txt"
echo 构建类型: Release >> "release\VERSION.txt"
echo. >> "release\VERSION.txt"
echo 文件说明: >> "release\VERSION.txt"
echo - single-file\: 单文件版本，无需安装.NET运行时 >> "release\VERSION.txt"
echo - portable\: 轻量版本，需要.NET 6.0运行时 >> "release\VERSION.txt"
echo.

:: 验证最终版本
echo 📊 验证最终版本...
for /f "delims=" %%i in ('powershell -Command "(Get-Content 'version.json' | ConvertFrom-Json).Version"') do set FINAL_VERSION=%%i
echo 最终版本: V%FINAL_VERSION%

if "%NEW_VERSION%"=="%FINAL_VERSION%" (
    echo ✅ 版本号保持一致 - 精确递增0.01
) else (
    echo ❌ 警告：版本号发生了意外变化！
    echo 预期：V%NEW_VERSION%
    echo 实际：V%FINAL_VERSION%
)
echo.

:: 显示发布结果
echo ========================================
echo 🎉 发布完成！
echo ========================================
echo.
echo 📁 发布目录: %CD%\release
echo 📊 版本号: V%FINAL_VERSION%
echo 📈 版本变化: V%CURRENT_VERSION% → V%FINAL_VERSION%
echo.
echo 💾 可用版本:
echo   - 单文件版: release\single-file\OrderWatch.exe
echo   - 轻量版:   release\portable\OrderWatch.exe
echo.
echo 📋 版本文件已自动包含在发布包中
echo.

:: 询问是否打开发布目录
set /p OPEN_FOLDER="🔍 是否打开发布目录？(Y/N): "
if /i "%OPEN_FOLDER%"=="Y" (
    explorer "release"
)

echo.
echo ✅ 发布脚本执行完成！
pause 