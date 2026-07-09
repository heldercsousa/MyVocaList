# Load environment variables from .env.local and persist to User scope
# Usage: . .\.claude\scripts\load-env.ps1

$envFile = "$PSScriptRoot\..\..\\.env.local"

if (-not (Test-Path $envFile)) {
    Write-Host "❌ .env.local not found at $envFile" -ForegroundColor Red
    Write-Host "   Copy .env.local.example to .env.local and fill in your keys" -ForegroundColor Yellow
    return
}

# Note: admin rights are NOT needed — User scope writes go to HKCU.

$count = 0
Get-Content $envFile | ForEach-Object {
    $_ = $_.Trim()
    # Skip empty lines and comments
    if ($_ -and -not $_.StartsWith('#')) {
        $parts = $_ -split '=', 2
        if ($parts.Count -eq 2) {
            $key = $parts[0].Trim()
            $value = $parts[1].Trim()
            # Set to User scope so Claude Code and all apps can see it
            [Environment]::SetEnvironmentVariable($key, $value, 'User')
            Write-Host "✓ Set $key" -ForegroundColor Green
            $count++
        }
    }
}

Write-Host "`nLoaded $count environment variables from .env.local (User scope)" -ForegroundColor Cyan
Write-Host "Note: Variables are persisted to the registry (HKCU) and will be available to newly launched apps." -ForegroundColor Green
Write-Host "IMPORTANT: Already-running processes keep their old environment snapshot." -ForegroundColor Yellow
Write-Host "  1. Close ALL terminal windows completely (not just this tab) - incl. VS Code / Visual Studio if you launch Claude Code from there." -ForegroundColor Yellow
Write-Host "  2. Open a fresh terminal from the Start menu / taskbar." -ForegroundColor Yellow
Write-Host "  3. Verify with: `$env:CONTEXT7_API_KEY  (should print your key), then start claude." -ForegroundColor Yellow
