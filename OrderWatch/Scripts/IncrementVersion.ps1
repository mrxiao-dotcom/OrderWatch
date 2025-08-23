# Version Increment Script
param(
    [string]$ProjectDir
)

$versionFilePath = Join-Path $ProjectDir "version.json"

try {
    # Read current version
    if (Test-Path $versionFilePath) {
        $versionContent = Get-Content $versionFilePath -Raw | ConvertFrom-Json
        $currentVersion = [decimal]$versionContent.Version
    } else {
        $currentVersion = 0.01
    }

    # Increment version
    $newVersion = $currentVersion + 0.01
    $newVersionString = $newVersion.ToString("F2")

    # Save new version
    $versionData = @{
        Version = $newVersionString
    }
    
    $versionData | ConvertTo-Json | Set-Content $versionFilePath -Encoding UTF8

    Write-Host "Version updated to: $newVersionString"
    
    # Update AssemblyInfo file
    $assemblyInfoPath = Join-Path $ProjectDir "Properties\AssemblyInfo.cs"
    if (Test-Path $assemblyInfoPath) {
        $assemblyContent = Get-Content $assemblyInfoPath
        $assemblyContent = $assemblyContent -replace 'AssemblyVersion\("[\d\.]+"\)', "AssemblyVersion(`"$newVersionString.0.0`")"
        $assemblyContent = $assemblyContent -replace 'AssemblyFileVersion\("[\d\.]+"\)', "AssemblyFileVersion(`"$newVersionString.0.0`")"
        $assemblyContent | Set-Content $assemblyInfoPath -Encoding UTF8
        Write-Host "AssemblyInfo updated"
    }
    
    Write-Host "Script completed successfully"
}
catch {
    Write-Warning "Version update failed: $_"
    exit 0  # Don't block compilation
}
