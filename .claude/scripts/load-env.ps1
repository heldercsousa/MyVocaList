# Load environment variables from .env.local and persist to User scope
# Usage: . .\.claude\scripts\load-env.ps1

$envFile = "$PSScriptRoot\..\..\\.env.local"

if (-not (Test-Path $envFile)) {
    Write-Host "❌ .env.local not found at $envFile" -ForegroundColor Red
    Write-Host "   Copy .env.local.example to .env.local and fill in your keys" -ForegroundColor Yellow
    return
}

# Check if running as admin (needed for warning message)
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

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
Write-Host "Note: Variables are now available to Claude Code and all other applications." -ForegroundColor Green
if (-not $isAdmin) {
    Write-Host "Tip: Restart Claude Code to pick up the new environment variables." -ForegroundColor Yellow
}
