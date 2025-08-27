param(
    [string]$ProjectDir
)

# 设置错误处理
$ErrorActionPreference = "SilentlyContinue"

try {
    Write-Host "🔧 开始版本号递增处理..." -ForegroundColor Green
    
    # 确定版本文件路径
    $versionFilePath = Join-Path (Split-Path $ProjectDir -Parent) "version.json"
    
    Write-Host "📁 项目目录: $ProjectDir" -ForegroundColor Yellow
    Write-Host "📄 版本文件路径: $versionFilePath" -ForegroundColor Yellow
    
    # 检查版本文件是否存在
    if (-not (Test-Path $versionFilePath)) {
        Write-Host "⚠️ 版本文件不存在，创建默认版本文件..." -ForegroundColor Yellow
        
        $defaultVersion = @{
            Version = "0.01"
            lastUpdate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        }
        
        $defaultVersionJson = $defaultVersion | ConvertTo-Json -Depth 2
        $defaultVersionJson | Out-File -FilePath $versionFilePath -Encoding UTF8 -Force
        Write-Host "✅ 创建默认版本文件: 0.01" -ForegroundColor Green
        exit 0
    }
    
    # 读取当前版本
    $versionContent = Get-Content $versionFilePath -Raw -Encoding UTF8
    $versionObj = $versionContent | ConvertFrom-Json
    
    $currentVersion = $versionObj.Version
    Write-Host "📊 当前版本: $currentVersion" -ForegroundColor Cyan
    
    # 递增版本号
    try {
        $versionDecimal = [decimal]$currentVersion
        $newVersionDecimal = $versionDecimal + 0.01
        $newVersion = $newVersionDecimal.ToString("F2")
        
        Write-Host "⬆️ 版本递增: $currentVersion -> $newVersion" -ForegroundColor Green
        
        # 更新版本对象
        $versionObj.Version = $newVersion
        $versionObj.lastUpdate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        
        # 保存新版本
        $newVersionJson = $versionObj | ConvertTo-Json -Depth 2
        $newVersionJson | Out-File -FilePath $versionFilePath -Encoding UTF8 -Force
        
        Write-Host "💾 版本已保存到: $versionFilePath" -ForegroundColor Green
        Write-Host "🎉 版本号递增完成: V$newVersion" -ForegroundColor Magenta
        
    } catch {
        Write-Host "❌ 版本号解析失败: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "🔄 保持当前版本: $currentVersion" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "❌ 版本处理失败: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "📝 详细错误信息:" -ForegroundColor Red
    Write-Host $_.Exception.ToString() -ForegroundColor Red
}

# 确保脚本正常退出
exit 0 