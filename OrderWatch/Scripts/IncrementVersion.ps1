# 版本号自动递增脚本
param(
    [string]$ProjectDir
)

$versionFilePath = Join-Path $ProjectDir "version.json"

try {
    # 读取当前版本
    if (Test-Path $versionFilePath) {
        $versionContent = Get-Content $versionFilePath -Raw | ConvertFrom-Json
        $currentVersion = [decimal]$versionContent.Version
    } else {
        $currentVersion = 0.01
    }

    # 递增版本号
    $newVersion = $currentVersion + 0.01
    $newVersionString = $newVersion.ToString("F2")

    # 保存新版本
    $versionData = @{
        Version = $newVersionString
    }
    
    $versionData | ConvertTo-Json | Set-Content $versionFilePath -Encoding UTF8

    Write-Host "版本号已更新到: $newVersionString"
    
    # 更新 AssemblyInfo 文件
    $assemblyInfoPath = Join-Path $ProjectDir "Properties\AssemblyInfo.cs"
    if (Test-Path $assemblyInfoPath) {
        $assemblyContent = Get-Content $assemblyInfoPath
        $assemblyContent = $assemblyContent -replace 'AssemblyVersion\("[\d\.]+"\)', "AssemblyVersion(`"$newVersionString.0.0`")"
        $assemblyContent = $assemblyContent -replace 'AssemblyFileVersion\("[\d\.]+"\)', "AssemblyFileVersion(`"$newVersionString.0.0`")"
        $assemblyContent | Set-Content $assemblyInfoPath -Encoding UTF8
        Write-Host "AssemblyInfo 已更新"
    }
}
catch {
    Write-Warning "版本号更新失败: $_"
    exit 0  # 不阻止编译
}
