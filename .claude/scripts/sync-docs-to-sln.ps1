<#
.SYNOPSIS
    Auto-registers a new Docs/ file into MyVocaList.sln.
    Called by the Claude Code PostToolUse hook on Write operations.
    Reads tool input JSON from stdin; the file_path is extracted from tool_input.file_path.
#>

$ErrorActionPreference = 'SilentlyContinue'

try {
    $input = $null
    $raw = [Console]::In.ReadToEnd()
    if ($raw) { $input = $raw | ConvertFrom-Json }
} catch { exit 0 }

$filePath = $input.tool_input.file_path
if (-not $filePath) { exit 0 }

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$slnPath  = Join-Path $repoRoot 'MyVocaList.sln'

# Normalize to relative path using backslashes
try {
    $rel = [System.IO.Path]::GetRelativePath($repoRoot, $filePath) -replace '/', '\'
} catch { exit 0 }

# Only act on files under Docs\
if (-not $rel.StartsWith('Docs\')) { exit 0 }

# Skip directories (no extension = likely a dir; also skip .sln itself)
if (-not [System.IO.Path]::GetExtension($rel)) { exit 0 }

$slnContent = Get-Content $slnPath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
if (-not $slnContent) { exit 0 }

# Already registered?
if ($slnContent.Contains($rel)) {
    exit 0
}

# Map path prefix → solution folder GUID (longest prefix wins — ordered dict)
$folderMap = [ordered]@{
    'Docs\DevEnv\SDD\plans\impl\'  = '{C32F2513-DA30-4ED9-BD64-50597B18E2D8}'
    'Docs\DevEnv\SDD\plans\'       = '{0D9EA0EB-5958-4F68-962E-398A2051CC33}'
    'Docs\DevEnv\SDD\'             = '{C9A5A169-C424-4871-AEC8-6E3E2F56F917}'
    'Docs\DevEnv\plans\'           = '{DD8EAF78-54CC-46D7-A9D6-A37F3C5CA9EB}'
    'Docs\DevEnv\'                 = '{300D3C1B-4E22-4CEE-85FF-453BD80607BD}'
    'Docs\Plans\'                  = '{D1C7C335-1E7C-4804-9A55-A5D160232353}'
    'Docs\specs\app-versioning\'   = '{893BCB2E-4C7D-4CF1-BDD4-DFC2A1C9A69B}'
    'Docs\specs\artists-songs\'    = '{C141C5C9-833C-4A26-96BF-3745A2DA1AD4}'
    'Docs\specs\m3-lists\'         = '{F58ED7A0-0186-4187-9FCF-78A464FB01C5}'
    'Docs\specs\persons\'          = '{D01D4F5A-EA21-4BEA-9808-B8FD795E79C7}'
    'Docs\specs\styles-structure\' = '{1093A402-3B6A-487E-B4AD-76087EDAC809}'
    'Docs\specs\venues\'           = '{9EA47D78-293E-41B7-872D-3B2B85E82710}'
    'Docs\specs\youtube-karaoke\'  = '{6851C0F2-5877-4E2F-89C0-9B82A6770BE0}'
    'Docs\Changelog\'              = '{E9A5FC59-0C8C-49B6-9845-7870FA3CD098}'
    'Docs\Design\'                 = '{2C8F9F52-D9DE-4986-BA2B-8901C35DE5F4}'
    'Docs\Management\'             = '{15F1DA03-2180-47BF-BC40-1BB457C97F9E}'
    'Docs\'                        = '{02EA681E-C7D8-13C7-8484-4AC65E1B71E8}'
}

$targetGuid = $null
foreach ($prefix in $folderMap.Keys) {
    if ($rel.StartsWith($prefix)) {
        $targetGuid = $folderMap[$prefix]
        break
    }
}

if (-not $targetGuid) {
    Write-Warning "sync-docs-to-sln: no folder mapping for '$rel' — add the folder to the map manually."
    exit 0
}

# Find the Project block for targetGuid and insert the file entry before EndProjectSection
$guidEscaped = [regex]::Escape($targetGuid)

# Pattern: find the block for this GUID, then find its EndProjectSection
$blockPattern = "(?s)(Project\(`"{2150E333[^`"]+`"`"\) = `"[^`"]+`", `"[^`"]+`", `"$guidEscaped`".*?)([ `t]*EndProjectSection)"
$entry = "`t`t$rel = $rel`r`n"

$newContent = [regex]::Replace($slnContent, $blockPattern, {
    param($m)
    $before = $m.Groups[1].Value
    $endSection = $m.Groups[2].Value

    # If block has no ProjectSection yet, add one
    if (-not $before.Contains("ProjectSection(SolutionItems)")) {
        $before = $before.TrimEnd() + "`r`n`tProjectSection(SolutionItems) = preProject`r`n"
        $endSection = "`t" + $endSection.TrimStart()
    }

    return $before + $entry + $endSection
}, 1)

if ($newContent -ne $slnContent) {
    Set-Content $slnPath $newContent -Encoding UTF8 -NoNewline
    Write-Host "sync-docs-to-sln: registered '$rel' into $targetGuid"
} else {
    Write-Warning "sync-docs-to-sln: pattern did not match for GUID $targetGuid — '$rel' not registered."
}
