# Version Increment Test Script
param(
    [string]$ProjectDir = "."
)

Write-Host "=== OrderWatch Version Increment Test ===" -ForegroundColor Green
Write-Host "Project Directory: $ProjectDir" -ForegroundColor Yellow

$versionFilePath = Join-Path $ProjectDir "version.json"

try {
    # Check if version file exists
    if (Test-Path $versionFilePath) {
        Write-Host "✓ Version file exists: $versionFilePath" -ForegroundColor Green
        
        # Read current version
        $versionContent = Get-Content $versionFilePath -Raw | ConvertFrom-Json
        $currentVersion = [decimal]$versionContent.Version
        Write-Host "✓ Current version: $currentVersion" -ForegroundColor Cyan
        
        # Calculate new version
        $newVersion = $currentVersion + 0.01
        $newVersionString = $newVersion.ToString("F2")
        Write-Host "✓ New version will be: $newVersionString" -ForegroundColor Cyan
        
        # Ask for confirmation
        $response = Read-Host "Continue to increment version? (Y/N)"
        if ($response -eq "Y" -or $response -eq "y") {
            # Save new version
            $versionData = @{
                Version = $newVersionString
            }
            
            $versionData | ConvertTo-Json | Set-Content $versionFilePath -Encoding UTF8
            Write-Host "✓ Version updated to: $newVersionString" -ForegroundColor Green
            
            # Show updated content
            $updatedContent = Get-Content $versionFilePath -Raw
            Write-Host "Updated version file content:" -ForegroundColor Yellow
            Write-Host $updatedContent -ForegroundColor White
        } else {
            Write-Host "Version increment cancelled" -ForegroundColor Yellow
        }
    } else {
        Write-Host "✗ Version file not found: $versionFilePath" -ForegroundColor Red
        Write-Host "Creating default version file..." -ForegroundColor Yellow
        
        $versionData = @{
            Version = "0.01"
        }
        
        $versionData | ConvertTo-Json | Set-Content $versionFilePath -Encoding UTF8
        Write-Host "✓ Default version file created with version: 0.01" -ForegroundColor Green
    }
}
catch {
    Write-Error "Script execution failed: $_"
    exit 1
}

Write-Host "=== Test Completed ===" -ForegroundColor Green
