# Load environment variables from .env.local into the current PowerShell session
# Usage: . .\.claude\scripts\load-env.ps1

$envFile = "$PSScriptRoot\..\..\\.env.local"

if (-not (Test-Path $envFile)) {
    Write-Host "❌ .env.local not found at $envFile" -ForegroundColor Red
    Write-Host "   Copy .env.local.example to .env.local and fill in your keys" -ForegroundColor Yellow
    return
}

$count = 0
Get-Content $envFile | ForEach-Object {
    $_ = $_.Trim()
    # Skip empty lines and comments
    if ($_ -and -not $_.StartsWith('#')) {
        $parts = $_ -split '=', 2
        if ($parts.Count -eq 2) {
            $key = $parts[0].Trim()
            $value = $parts[1].Trim()
            [Environment]::SetEnvironmentVariable($key, $value, 'Process')
            Write-Host "✓ Set $key" -ForegroundColor Green
            $count++
        }
    }
}

Write-Host "`nLoaded $count environment variables from .env.local" -ForegroundColor Cyan
Write-Host "Note: These are set for this PowerShell session only. Run this script each time you open PowerShell." -ForegroundColor Yellow
