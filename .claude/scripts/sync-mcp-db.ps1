# Sync the SQLite MCP database (.claude/MyVocaList.db) to the latest EF schema.
# Called by the PostToolUse hook after `dotnet ef database update` (via hook-bash-filter.py).
# Infra is both project and startup project: the app project cannot be the EF startup
# (no desktop deps.json for net10.0-android). The connection path must be absolute,
# because EF resolves a relative Data Source against the Infra folder.
$proj = $env:CLAUDE_PROJECT_DIR
if (-not $proj) { exit 0 }
Push-Location $proj
try {
    $out = & dotnet ef database update -p Infra -s Infra --connection "Data Source=$proj/.claude/MyVocaList.db" 2>&1
    if ($LASTEXITCODE -eq 0) { Write-Host 'SQLite MCP DB synced with latest schema' }
    else { Write-Host "SQLite MCP DB sync FAILED (exit $LASTEXITCODE): $(($out | Select-Object -Last 1))" }
}
finally { Pop-Location }
exit 0
