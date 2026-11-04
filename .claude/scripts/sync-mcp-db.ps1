# Sync the SQLite MCP database (.claude/MyVocaList.db) to the latest EF schema.
# Called by the PostToolUse hook after `dotnet ef database update` (via hook-bash-filter.py).
$proj = $env:CLAUDE_PROJECT_DIR
if (-not $proj) { exit 0 }
& dotnet ef database update --connection "Data Source=$proj/.claude/MyVocaList.db" 2>$null
if ($?) { Write-Host 'SQLite MCP DB synced with latest schema' }
exit 0
