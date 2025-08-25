@echo off
REM ==============================================================================
REM Version Update Script for OrderWatch Project
REM ==============================================================================
REM This script automatically increments the version number in version.json
REM The version is displayed in the application window title
REM 
REM Files involved:
REM - version.json        : Version number storage (this file gets updated)
REM - VersionManager.cs   : Reads version.json and formats it for display
REM - TestViewModel.cs    : Sets window title using VersionManager
REM ==============================================================================

echo.
echo ========================================
echo   OrderWatch Version Update Tool
echo ========================================
echo.

REM Check if version.json exists
if not exist "version.json" (
    echo ERROR: version.json file not found!
    echo Please run this script from the project root directory.
    pause
    exit /b 1
)

echo Current version info:
type version.json
echo.
echo Updating version number...

REM Update version using PowerShell
powershell -Command "$v = (Get-Content 'version.json' | ConvertFrom-Json); $old = $v.Version; $new = ([decimal]$v.Version + 0.01).ToString('0.00'); $v.Version = $new; $v.lastUpdate = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'; $v | ConvertTo-Json | Set-Content 'version.json' -Encoding UTF8; Write-Host 'Version updated:' $old '->' $new -ForegroundColor Green"

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to update version!
    pause
    exit /b 1
)

echo.
echo Updated version info:
type version.json
echo.
echo ========================================
echo Version updated successfully!
echo ========================================
echo.
echo Copying version file to output directories...
if exist "OrderWatch\bin\Debug\net6.0-windows\" (
    copy version.json OrderWatch\bin\Debug\net6.0-windows\ >nul 2>&1
    echo Version file copied to Debug output directory.
)
if exist "OrderWatch\bin\Release\net6.0-windows\" (
    copy version.json OrderWatch\bin\Release\net6.0-windows\ >nul 2>&1
    echo Version file copied to Release output directory.
)
echo.
echo To see the new version in the application:
echo 1. Build the project: dotnet build
echo 2. Run the application: dotnet run
echo 3. Check the window title
echo.
pause
