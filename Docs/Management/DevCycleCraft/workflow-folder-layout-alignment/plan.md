# Docs/ Folder Full Alignment — .sln Sync, BACKLOG Audit & Auto-Registration Hook

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every file under `Docs/` visible in Visual Studio via the `.sln`, fix BACKLOG gaps, and add a hook that auto-registers new `Docs/` files into the solution going forward.

**Architecture:** Three independent work streams — (1) `.sln` file surgery to mirror disk structure exactly, (2) `BACKLOG.md` update for untracked plans, (3) a PostToolUse hook + PowerShell script for ongoing auto-registration.

**Tech Stack:** PowerShell, Visual Studio `.sln` text format (GUID-based solution folders), Claude Code `settings.json` hook API.

---

## Audit Findings

### A — SLN orphaned references (in .sln but NOT on disk — must be removed)

Ten files referenced in the `Plans` solution folder (nested under `SDD`) that do not exist on disk:
- `Docs\DevEnv\SDD\plans\S1_opportunities.md` through `S10_opportunities.md` (10 entries)

One duplicate entry: `Docs\DevEnv\SETUP_QUICKSTART.md` appears in both **"Itens de Solução"** and **"DevEnv"** — keep only in DevEnv.

### B — Files on disk NOT in .sln (must be added)

| Disk path | Target solution folder |
|-----------|----------------------|
| `Docs\DevEnv\workflow-layout-findings.md` | DevEnv |
| `Docs\DevEnv\plans\compressed-plotting-rocket.md` | DevEnv\plans (new folder) |
| `Docs\DevEnv\plans\drifting-sniffing-allen.md` | DevEnv\plans (new folder) |
| `Docs\Plans\2026-03-06-solution-structure-refactor.md` | Plans (new folder) |
| `Docs\Plans\2026-03-10-md3-appbar-components.md` | Plans (new folder) |
| `Docs\specs\app-versioning\design.md` | specs\app-versioning (new folder) |
| `Docs\specs\m3-lists\design.md` | specs\m3-lists (new folder) |
| `Docs\specs\persons\autocomplete-design.md` | specs\persons (new folder) |
| `Docs\specs\persons\design.md` | specs\persons (new folder) |
| `Docs\specs\persons\requirements.md` | specs\persons (new folder) |
| `Docs\specs\persons\tasks.md` | specs\persons (new folder) |
| `Docs\specs\styles-structure\design.md` | specs\styles-structure (new folder) |
| `Docs\specs\venues\design.md` | specs\venues (new folder) |
| `Docs\specs\venues\requirements.md` | specs\venues (new folder) |
| `Docs\specs\venues\tasks.md` | specs\venues (new folder) |
| `Docs\specs\youtube-karaoke\design.md` | specs\youtube-karaoke (new folder) |
| `Docs\specs\youtube-karaoke\requirements.md` | specs\youtube-karaoke (new folder) |
| `Docs\specs\youtube-karaoke\tasks.md` | specs\youtube-karaoke (new folder) |
| `Docs\superpowers\plans\2026-03-11-m3-lists.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\2026-03-29-venues-md3-rebuild.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\2026-03-31-styles-structure.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\2026-04-02-toolbar-fab-vibrant.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\2026-04-06-autocomplete-field.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\2026-04-07-person-crud.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\2026-04-23-artists-songs-catalog.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\2026-05-17-youtube-karaoke.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\2026-05-18-app-versioning.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\cheeky-popping-whale.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\curious-sniffing-grove.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\hashed-sprouting-pebble.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\jolly-conjuring-coral.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\recursive-painting-melody.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\validated-noodling-island-task-log.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\validated-noodling-island.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\zippy-cuddling-penguin-handoff.md` | superpowers\plans (new folder) |
| `Docs\superpowers\plans\zippy-cuddling-penguin.md` | superpowers\plans (new folder) |

### C — BACKLOG.md missing Dev Cycle Craft entries

| File | What it is | BACKLOG status |
|------|-----------|----------------|
| `Docs/Plans/2026-03-06-solution-structure-refactor.md` | Solution structure refactor | ✅ Done 2026-03 |
| `Docs/Plans/2026-03-10-md3-appbar-components.md` | MD3 App Bar components | ✅ Done 2026-03 |
| `superpowers/plans/2026-04-07-person-crud.md` | Person CRUD plan | existing Done entry, no plan file ref |

Random-named plans are already captured by existing named BACKLOG entries — they need no new rows, only the missing plan-file reference in those existing rows where absent.

### D — New solution folders required (GUIDs to be generated at implementation time)

| Disk path | Solution folder display name | Parent folder |
|-----------|----------------------------|---------------|
| `Docs\DevEnv\plans\` | plans | DevEnv {300D3C1B-4E22-4CEE-85FF-453BD80607BD} |
| `Docs\Plans\` | Plans | Docs {02EA681E-C7D8-13C7-8484-4AC65E1B71E8} |
| `Docs\specs\app-versioning\` | app-versioning | Specs {4CA06FF1-8789-445B-8AF3-32D4643CDBB3} |
| `Docs\specs\m3-lists\` | m3-lists | Specs |
| `Docs\specs\persons\` | persons | Specs |
| `Docs\specs\styles-structure\` | styles-structure | Specs |
| `Docs\specs\venues\` | venues | Specs |
| `Docs\specs\youtube-karaoke\` | youtube-karaoke | Specs |
| `Docs\superpowers\` | superpowers | Docs |
| `Docs\superpowers\plans\` | plans | superpowers |

---

## Critical files

| File | Role |
|------|------|
| `MyVocaList.sln` | Solution file — all edits happen here |
| `Docs/BACKLOG.md` | Backlog — 3 updates |
| `.claude/settings.json` | Hook registration |
| `.claude/scripts/sync-docs-to-sln.ps1` | New script — auto-registers Docs/ files |

---

## Task 1 — Generate new GUIDs (prerequisite, done once)

**Files:** `MyVocaList.sln`

- [ ] **Step 1: Generate 10 GUIDs for new solution folders**

Run in PowerShell from the repo root:

```powershell
1..10 | ForEach-Object { "{$([Guid]::NewGuid().ToString().ToUpper())}" }
```

Record the 10 GUIDs in this order (fill in the output values before proceeding):

```
GUID_DEVENV_PLANS     = <generated>
GUID_PLANS_LEGACY     = <generated>
GUID_SPECS_APPVER     = <generated>
GUID_SPECS_M3LISTS    = <generated>
GUID_SPECS_PERSONS    = <generated>
GUID_SPECS_STYLES     = <generated>
GUID_SPECS_VENUES     = <generated>
GUID_SPECS_YTKARAOKE  = <generated>
GUID_SUPERPOWERS      = <generated>
GUID_SP_PLANS         = <generated>
```

---

## Task 2 — Remove orphaned .sln entries

**Files:** `MyVocaList.sln`

- [ ] **Step 2: Remove 10 S*_opportunities.md lines from the Plans ProjectSection**

In `MyVocaList.sln`, locate the `Plans` solution folder ProjectSection block (GUID `{0D9EA0EB-5958-4F68-962E-398A2051CC33}`). Remove these 10 lines (they reference files that don't exist):

```
		Docs\DevEnv\SDD\plans\S10_opportunities.md = Docs\DevEnv\SDD\plans\S10_opportunities.md
		Docs\DevEnv\SDD\plans\S1_opportunities.md = Docs\DevEnv\SDD\plans\S1_opportunities.md
		Docs\DevEnv\SDD\plans\S2_opportunities.md = Docs\DevEnv\SDD\plans\S2_opportunities.md
		Docs\DevEnv\SDD\plans\S3_opportunities.md = Docs\DevEnv\SDD\plans\S3_opportunities.md
		Docs\DevEnv\SDD\plans\S4_opportunities.md = Docs\DevEnv\SDD\plans\S4_opportunities.md
		Docs\DevEnv\SDD\plans\S5_opportunities.md = Docs\DevEnv\SDD\plans\S5_opportunities.md
		Docs\DevEnv\SDD\plans\S6_opportunities.md = Docs\DevEnv\SDD\plans\S6_opportunities.md
		Docs\DevEnv\SDD\plans\S7_opportunities.md = Docs\DevEnv\SDD\plans\S7_opportunities.md
		Docs\DevEnv\SDD\plans\S8_opportunities.md = Docs\DevEnv\SDD\plans\S8_opportunities.md
		Docs\DevEnv\SDD\plans\S9_opportunities.md = Docs\DevEnv\SDD\plans\S9_opportunities.md
```

- [ ] **Step 3: Remove duplicate SETUP_QUICKSTART.md from "Itens de Solução"**

In the `Itens de Solução` ProjectSection block (GUID `{380A7511-A354-6D7A-0CC0-8FA1F1BA7B6C}`), remove this single line:

```
		Docs\DevEnv\SETUP_QUICKSTART.md = Docs\DevEnv\SETUP_QUICKSTART.md
```

(Keep the entry in the DevEnv folder block — it belongs there.)

- [ ] **Step 4: Verify .sln still loads (sanity check)**

```powershell
dotnet sln MyVocaList.sln list
```

Expected: list of all .csproj projects with no errors. Solution folders don't appear in this output but errors do.

---

## Task 3 — Add missing file to existing DevEnv folder

**Files:** `MyVocaList.sln`

- [ ] **Step 5: Add workflow-layout-findings.md to DevEnv ProjectSection**

In the DevEnv ProjectSection block (GUID `{300D3C1B-4E22-4CEE-85FF-453BD80607BD}`), add:

```
		Docs\DevEnv\workflow-layout-findings.md = Docs\DevEnv\workflow-layout-findings.md
```

---

## Task 4 — Add 10 new solution folder Project blocks

**Files:** `MyVocaList.sln`

Add the following Project blocks immediately before the `Global` section. Use the GUIDs recorded in Task 1.

- [ ] **Step 6: Add DevEnv\plans\ folder block**

```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "plans", "plans", "GUID_DEVENV_PLANS"
	ProjectSection(SolutionItems) = preProject
		Docs\DevEnv\plans\compressed-plotting-rocket.md = Docs\DevEnv\plans\compressed-plotting-rocket.md
		Docs\DevEnv\plans\drifting-sniffing-allen.md = Docs\DevEnv\plans\drifting-sniffing-allen.md
	EndProjectSection
EndProject
```

- [ ] **Step 7: Add Plans\ (legacy) folder block**

```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Plans", "Plans", "GUID_PLANS_LEGACY"
	ProjectSection(SolutionItems) = preProject
		Docs\Plans\2026-03-06-solution-structure-refactor.md = Docs\Plans\2026-03-06-solution-structure-refactor.md
		Docs\Plans\2026-03-10-md3-appbar-components.md = Docs\Plans\2026-03-10-md3-appbar-components.md
	EndProjectSection
EndProject
```

- [ ] **Step 8: Add specs\app-versioning\ folder block**

```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "app-versioning", "app-versioning", "GUID_SPECS_APPVER"
	ProjectSection(SolutionItems) = preProject
		Docs\specs\app-versioning\design.md = Docs\specs\app-versioning\design.md
	EndProjectSection
EndProject
```

- [ ] **Step 9: Add specs\m3-lists\ folder block**

```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "m3-lists", "m3-lists", "GUID_SPECS_M3LISTS"
	ProjectSection(SolutionItems) = preProject
		Docs\specs\m3-lists\design.md = Docs\specs\m3-lists\design.md
	EndProjectSection
EndProject
```

- [ ] **Step 10: Add specs\persons\ folder block**

```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "persons", "persons", "GUID_SPECS_PERSONS"
	ProjectSection(SolutionItems) = preProject
		Docs\specs\persons\autocomplete-design.md = Docs\specs\persons\autocomplete-design.md
		Docs\specs\persons\design.md = Docs\specs\persons\design.md
		Docs\specs\persons\requirements.md = Docs\specs\persons\requirements.md
		Docs\specs\persons\tasks.md = Docs\specs\persons\tasks.md
	EndProjectSection
EndProject
```

- [ ] **Step 11: Add specs\styles-structure\ folder block**

```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "styles-structure", "styles-structure", "GUID_SPECS_STYLES"
	ProjectSection(SolutionItems) = preProject
		Docs\specs\styles-structure\design.md = Docs\specs\styles-structure\design.md
	EndProjectSection
EndProject
```

- [ ] **Step 12: Add specs\venues\ folder block**

```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "venues", "venues", "GUID_SPECS_VENUES"
	ProjectSection(SolutionItems) = preProject
		Docs\specs\venues\design.md = Docs\specs\venues\design.md
		Docs\specs\venues\requirements.md = Docs\specs\venues\requirements.md
		Docs\specs\venues\tasks.md = Docs\specs\venues\tasks.md
	EndProjectSection
EndProject
```

- [ ] **Step 13: Add specs\youtube-karaoke\ folder block**

```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "youtube-karaoke", "youtube-karaoke", "GUID_SPECS_YTKARAOKE"
	ProjectSection(SolutionItems) = preProject
		Docs\specs\youtube-karaoke\design.md = Docs\specs\youtube-karaoke\design.md
		Docs\specs\youtube-karaoke\requirements.md = Docs\specs\youtube-karaoke\requirements.md
		Docs\specs\youtube-karaoke\tasks.md = Docs\specs\youtube-karaoke\tasks.md
	EndProjectSection
EndProject
```

- [ ] **Step 14: Add superpowers\ folder block (parent, no direct files)**

```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "superpowers", "superpowers", "GUID_SUPERPOWERS"
EndProject
```

- [ ] **Step 15: Add superpowers\plans\ folder block (all 18 plan files)**

```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "plans", "plans", "GUID_SP_PLANS"
	ProjectSection(SolutionItems) = preProject
		Docs\superpowers\plans\2026-03-11-m3-lists.md = Docs\superpowers\plans\2026-03-11-m3-lists.md
		Docs\superpowers\plans\2026-03-29-venues-md3-rebuild.md = Docs\superpowers\plans\2026-03-29-venues-md3-rebuild.md
		Docs\superpowers\plans\2026-03-31-styles-structure.md = Docs\superpowers\plans\2026-03-31-styles-structure.md
		Docs\superpowers\plans\2026-04-02-toolbar-fab-vibrant.md = Docs\superpowers\plans\2026-04-02-toolbar-fab-vibrant.md
		Docs\superpowers\plans\2026-04-06-autocomplete-field.md = Docs\superpowers\plans\2026-04-06-autocomplete-field.md
		Docs\superpowers\plans\2026-04-07-person-crud.md = Docs\superpowers\plans\2026-04-07-person-crud.md
		Docs\superpowers\plans\2026-04-23-artists-songs-catalog.md = Docs\superpowers\plans\2026-04-23-artists-songs-catalog.md
		Docs\superpowers\plans\2026-05-17-youtube-karaoke.md = Docs\superpowers\plans\2026-05-17-youtube-karaoke.md
		Docs\superpowers\plans\2026-05-18-app-versioning.md = Docs\superpowers\plans\2026-05-18-app-versioning.md
		Docs\superpowers\plans\cheeky-popping-whale.md = Docs\superpowers\plans\cheeky-popping-whale.md
		Docs\superpowers\plans\curious-sniffing-grove.md = Docs\superpowers\plans\curious-sniffing-grove.md
		Docs\superpowers\plans\hashed-sprouting-pebble.md = Docs\superpowers\plans\hashed-sprouting-pebble.md
		Docs\superpowers\plans\jolly-conjuring-coral.md = Docs\superpowers\plans\jolly-conjuring-coral.md
		Docs\superpowers\plans\recursive-painting-melody.md = Docs\superpowers\plans\recursive-painting-melody.md
		Docs\superpowers\plans\validated-noodling-island-task-log.md = Docs\superpowers\plans\validated-noodling-island-task-log.md
		Docs\superpowers\plans\validated-noodling-island.md = Docs\superpowers\plans\validated-noodling-island.md
		Docs\superpowers\plans\zippy-cuddling-penguin-handoff.md = Docs\superpowers\plans\zippy-cuddling-penguin-handoff.md
		Docs\superpowers\plans\zippy-cuddling-penguin.md = Docs\superpowers\plans\zippy-cuddling-penguin.md
	EndProjectSection
EndProject
```

---

## Task 5 — Wire NestedProjects for the 10 new folders

**Files:** `MyVocaList.sln`

- [ ] **Step 16: Add NestedProjects entries**

In the `GlobalSection(NestedProjects)` block, add these lines (substitute real GUIDs):

```
		GUID_DEVENV_PLANS = {300D3C1B-4E22-4CEE-85FF-453BD80607BD}
		GUID_PLANS_LEGACY = {02EA681E-C7D8-13C7-8484-4AC65E1B71E8}
		GUID_SPECS_APPVER = {4CA06FF1-8789-445B-8AF3-32D4643CDBB3}
		GUID_SPECS_M3LISTS = {4CA06FF1-8789-445B-8AF3-32D4643CDBB3}
		GUID_SPECS_PERSONS = {4CA06FF1-8789-445B-8AF3-32D4643CDBB3}
		GUID_SPECS_STYLES = {4CA06FF1-8789-445B-8AF3-32D4643CDBB3}
		GUID_SPECS_VENUES = {4CA06FF1-8789-445B-8AF3-32D4643CDBB3}
		GUID_SPECS_YTKARAOKE = {4CA06FF1-8789-445B-8AF3-32D4643CDBB3}
		GUID_SUPERPOWERS = {02EA681E-C7D8-13C7-8484-4AC65E1B71E8}
		GUID_SP_PLANS = GUID_SUPERPOWERS
```

- [ ] **Step 17: Validate solution structure**

```powershell
dotnet sln MyVocaList.sln list
```

Expected: no errors. Open `MyVocaList.sln` in Visual Studio and verify the Solution Explorer shows the full folder tree matching disk layout.

---

## Task 6 — BACKLOG.md update

**Files:** `Docs/BACKLOG.md`

- [ ] **Step 18: Add two missing Done entries to Dev Cycle Craft table**

Insert before the "M3 Lists" row (earliest entry, keep chronological order):

```markdown
| 2026-03 | Solution Structure Refactor | ✅ Done | Move service interfaces to Domain, delete IDatabaseInit, reorganize MAUI project. Plan: `Docs/Plans/2026-03-06-solution-structure-refactor.md` |
| 2026-03 | MD3 App Bar Components | ✅ Done | SmallAppBar + SearchAppBar ContentView components. Plan: `Docs/Plans/2026-03-10-md3-appbar-components.md` |
```

- [ ] **Step 19: Add plan file reference to Person CRUD Done row**

Update the `Autocomplete field` Done row to add a plan file reference. Find:
```
| 2026-04 | Autocomplete field | ✅ Done | `Docs/superpowers/plans/2026-04-06-autocomplete-field.md` |
```
And add the Person CRUD plan reference to the existing **Person CRUD** business feature row's Notes field, or note in Dev Cycle Craft that `Docs/superpowers/plans/2026-04-07-person-crud.md` was its implementation plan.

Actually: add a reference line in the existing "Person CRUD ✅ Done" **Business Features** row Notes cell:
```
Plan: `Docs/superpowers/plans/2026-04-07-person-crud.md`
```

---

## Task 7 — Create auto-registration script

**Files:** `.claude/scripts/sync-docs-to-sln.ps1` (new)

- [ ] **Step 20: Create the scripts directory and script file**

```powershell
New-Item -ItemType Directory -Force -Path ".claude\scripts"
```

Create `.claude/scripts/sync-docs-to-sln.ps1`:

```powershell
<#
.SYNOPSIS
    Auto-registers a new Docs/ file into MyVocaList.sln.
    Called by the Claude Code PostToolUse hook on Write operations.
.PARAMETER FilePath
    Absolute or relative path of the newly created file.
#>
param([string]$FilePath)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent | Split-Path -Parent
$slnPath  = Join-Path $repoRoot 'MyVocaList.sln'

# Normalize to relative path using backslashes
$rel = [System.IO.Path]::GetRelativePath($repoRoot, $FilePath) -replace '/', '\'

# Only act on files under Docs\
if (-not $rel.StartsWith('Docs\')) { exit 0 }

$slnContent = Get-Content $slnPath -Raw

# Already registered?
if ($slnContent -match [regex]::Escape($rel)) {
    Write-Host "sync-docs-to-sln: already registered — $rel"
    exit 0
}

# Map path prefix → solution folder GUID
# Update this table whenever a new solution folder is added.
$folderMap = [ordered]@{
    'Docs\DevEnv\SDD\plans\impl\'  = '{C32F2513-DA30-4ED9-BD64-50597B18E2D8}'
    'Docs\DevEnv\SDD\plans\'       = '{0D9EA0EB-5958-4F68-962E-398A2051CC33}'
    'Docs\DevEnv\SDD\'             = '{C9A5A169-C424-4871-AEC8-6E3E2F56F917}'
    'Docs\DevEnv\plans\'           = 'GUID_DEVENV_PLANS'   # replaced at implementation time
    'Docs\DevEnv\'                 = '{300D3C1B-4E22-4CEE-85FF-453BD80607BD}'
    'Docs\Plans\'                  = 'GUID_PLANS_LEGACY'   # replaced at implementation time
    'Docs\specs\app-versioning\'   = 'GUID_SPECS_APPVER'   # replaced at implementation time
    'Docs\specs\artists-songs\'    = '{C141C5C9-833C-4A26-96BF-3745A2DA1AD4}'
    'Docs\specs\m3-lists\'         = 'GUID_SPECS_M3LISTS'  # replaced at implementation time
    'Docs\specs\persons\'          = 'GUID_SPECS_PERSONS'  # replaced at implementation time
    'Docs\specs\styles-structure\' = 'GUID_SPECS_STYLES'   # replaced at implementation time
    'Docs\specs\venues\'           = 'GUID_SPECS_VENUES'   # replaced at implementation time
    'Docs\specs\youtube-karaoke\'  = 'GUID_SPECS_YTKARAOKE'# replaced at implementation time
    'Docs\superpowers\plans\'      = 'GUID_SP_PLANS'       # replaced at implementation time
    'Docs\Changelog\'              = '{E9A5FC59-0C8C-49B6-9845-7870FA3CD098}'
    'Docs\Design\'                 = '{2C8F9F52-D9DE-4986-BA2B-8901C35DE5F4}'
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
    Write-Warning "sync-docs-to-sln: no folder mapping found for $rel — add manually."
    exit 0
}

# Insert the file entry before EndProjectSection of the target folder
$entry = "`t`t$rel = $rel"
$pattern = "(?s)(ProjectSection\(SolutionItems\) = preProject.*?)([ `t]*EndProjectSection)"
$replacement = { param($m) "$($m.Groups[1].Value)`n$entry`n$($m.Groups[2].Value)" }

# Find the correct Project block (match by GUID)
$guidEscaped = [regex]::Escape($targetGuid)
$blockPattern = "(?s)(Project\(`"{2150E333[^`"]+`"\`"\) = `"[^`"]+`", `"[^`"]+`", `"$guidEscaped`".*?EndProject)"

if ($slnContent -match $blockPattern) {
    $block = $Matches[1]
    $newBlock = [regex]::Replace($block, $pattern, $replacement)
    $slnContent = $slnContent.Replace($block, $newBlock)
    Set-Content $slnPath $slnContent -NoNewline
    Write-Host "sync-docs-to-sln: registered $rel into $targetGuid"
} else {
    Write-Warning "sync-docs-to-sln: solution folder block $targetGuid not found — file not registered."
}
```

**IMPORTANT:** After Task 5 completes and real GUIDs are known, replace all `GUID_*` placeholder strings in this script with the actual GUID values recorded in Task 1.

---

## Task 8 — Add PostToolUse hook to settings.json

**Files:** `.claude/settings.json`

- [ ] **Step 21: Add new PostToolUse hook entry**

In `.claude/settings.json`, inside the `"PostToolUse"` hooks array, add a new hook entry after the last existing one:

```json
{
  "matcher": "Write",
  "hooks": [
    {
      "type": "command",
      "command": "pwsh -NoProfile -NonInteractive -File .claude/scripts/sync-docs-to-sln.ps1 -FilePath \"$TOOL_INPUT_FILE_PATH\""
    }
  ]
}
```

> **Note on hook variable:** The exact environment variable name for the file path depends on the Claude Code hook API. Check the existing PostToolUse Hook 1 (which writes to `changed-files.txt`) to confirm the correct variable name for the written file path — reuse the same variable here.

- [ ] **Step 22: Smoke-test the hook**

Create a throwaway file to trigger the hook:

```powershell
echo "test" > Docs\specs\venues\test-hook.md
```

Verify `MyVocaList.sln` now contains `Docs\specs\venues\test-hook.md`.

Then delete the throwaway file:

```powershell
Remove-Item Docs\specs\venues\test-hook.md
```

And remove the entry from `MyVocaList.sln` manually.

---

## Task 9 — Commit

- [ ] **Step 23: Commit all changes**

```powershell
git add MyVocaList.sln Docs/BACKLOG.md .claude/settings.json .claude/scripts/sync-docs-to-sln.ps1
git commit -m @'
chore: full Docs/ sync to .sln + BACKLOG audit + auto-registration hook

- .sln: removed 10 orphaned S*_opportunities.md entries; removed duplicate
  SETUP_QUICKSTART entry; added 10 new solution folders (DevEnv\plans, Plans,
  specs\app-versioning, specs\m3-lists, specs\persons, specs\styles-structure,
  specs\venues, specs\youtube-karaoke, superpowers, superpowers\plans) with all
  36 previously unregistered Docs/ files.
- BACKLOG.md: added Solution Structure Refactor and MD3 App Bar Components Done
  entries; added Person CRUD plan file reference.
- Hook: new PostToolUse Write hook calls sync-docs-to-sln.ps1 to auto-register
  any future file created under Docs/ into the correct .sln solution folder.

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
'@
```

---

## Verification

1. Open `MyVocaList.sln` in Visual Studio — Solution Explorer must show the full `Docs/` tree matching disk layout.
2. Every file listed in Audit Finding B must appear in the Solution Explorer under its correct folder.
3. No file from Finding A (orphaned) should appear.
4. `BACKLOG.md` diff shows two new Done rows and one updated Notes cell.
5. Create `Docs/specs/venues/test-auto.md` via the Write tool — within seconds, `MyVocaList.sln` must contain the new entry. Delete the test file and remove from .sln.
