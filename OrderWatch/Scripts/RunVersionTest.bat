@echo off
chcp 65001 >nul
echo ========================================
echo OrderWatch Version Increment Test
echo ========================================
echo.

REM Switch to script directory
cd /d "%~dp0"

REM Execute PowerShell script
echo Executing version increment test...
powershell -ExecutionPolicy Bypass -NoProfile -File "TestVersion.ps1" -ProjectDir ".."

echo.
echo Press any key to exit...
pause >nul
