@echo off
REM Simple version increment for VS2022
REM No Chinese characters to avoid encoding issues

powershell -Command "& {$v = (Get-Content 'version.json' | ConvertFrom-Json); $nv = [decimal]$v.version + 0.01; $v.version = $nv.ToString('0.00'); $v.lastUpdate = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'; $v | ConvertTo-Json | Set-Content 'version.json' -Encoding UTF8; Write-Host 'Version updated to:' $v.version -ForegroundColor Green}"

echo.
echo Version file updated successfully!
echo Check the window title after building.
pause
