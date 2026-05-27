# Plan: Findings.md Canonicalization + Two New Process Rules

> Light-ceremony tasks (docs/rules updates, no spec needed). Commit prefix: `chore:` for file moves, `amend:` for rule changes.

---

## Context

Three things to accomplish:

1. **Immediate fix** — `Docs/DevEnv/workflow-layout-findings.md` is in a non-canonical location. It belongs at `Docs/Management/DevCycleCraft/workflow-folder-layout-alignment/findings.md` per the routing rule. It also needs to be registered in `MyVocaList.sln` so it's visible in VS Solution Explorer.

2. **New rule: VS Solution file registration** — No guideline currently says "register new doc files in .sln." This omission caused the findings.md to be invisible in VS. A mandatory pre-commit rule is needed.

3. **New rule: Proactive BACKLOG entry creation** — No guideline currently says "when the user asks for something not yet tracked in BACKLOG.md, add an entry before proceeding." This caused activities like today's to go untracked until asked.

Both rules are new DevCycleCraft activities, so two BACKLOG entries must be added.

---

## Write-Protection Check (do this first)

Read `.claude/settings.json` → look for `permissions.deny` entries:
- `"Edit(CLAUDE.md)"` / `"Write(CLAUDE.md)"`
- `"Edit(.claude/rules/*.md)"` / `"Write(.claude/rules/*.md)"`

If present, remove them before editing those files. Restore them after all phases complete (commit: `amend: re-apply write protection to CLAUDE.md and rules files`).

---

## Phase 1 — Move findings.md + update .sln + update BACKLOG reference

**Files touched:** `Docs/DevEnv/workflow-layout-findings.md` (deleted), `Docs/Management/DevCycleCraft/workflow-folder-layout-alignment/findings.md` (new), `MyVocaList.sln`, `Docs/Management/BACKLOG.md`

### Step 1.1 — Git-move the file
```powershell
git mv "Docs\DevEnv\workflow-layout-findings.md" "Docs\Management\DevCycleCraft\workflow-folder-layout-alignment\findings.md"
```

### Step 1.2 — Add findings.md to MyVocaList.sln

In `MyVocaList.sln`, the `workflow-folder-layout-alignment` Solution Folder is at lines 203–207:
```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "workflow-folder-layout-alignment", ...
	ProjectSection(SolutionItems) = preProject
		Docs\Management\DevCycleCraft\workflow-folder-layout-alignment\plan.md = Docs\Management\DevCycleCraft\workflow-folder-layout-alignment\plan.md
	EndProjectSection
EndProject
```

Add one line after `plan.md = plan.md`:
```
		Docs\Management\DevCycleCraft\workflow-folder-layout-alignment\findings.md = Docs\Management\DevCycleCraft\workflow-folder-layout-alignment\findings.md
```

### Step 1.3 — Update BACKLOG.md reference (line 64)

Change:
```
Findings: `Docs/DevEnv/workflow-layout-findings.md` · Plan: `Docs/Management/DevCycleCraft/workflow-folder-layout-alignment/plan.md`
```
To:
```
Findings: `Docs/Management/DevCycleCraft/workflow-folder-layout-alignment/findings.md` · Plan: `Docs/Management/DevCycleCraft/workflow-folder-layout-alignment/plan.md`
```

### Step 1.4 — Commit
```
chore: move workflow-layout-findings.md to canonical location, register in .sln
```

---

## Phase 2 — Add two new BACKLOG entries

**File touched:** `Docs/Management/BACKLOG.md`

Add two rows to the **Dev Cycle Craft** table (insert after the "Workflow & Folder Layout Alignment" row):

```markdown
| 2026-05 | **VS Solution File Registration Rule** | 🟡 In Progress | Mandatory rule: any doc file visible in VS must be registered in .sln before commit. See `workflow.md` and `constraints-registry.md`. |
| 2026-05 | **Proactive BACKLOG Entry Rule** | 🟡 In Progress | Agents must add brief BACKLOG entries for untracked work identified during sessions. See `workflow.md` Rule 1. |
```

### Step 2.1 — Commit
```
chore: add VS solution registration + proactive BACKLOG entry activities to BACKLOG.md
```

---

## Phase 3 — VS Solution File Registration Rule

**Files touched:** `.claude/rules/workflow.md`, `.claude/rules/constraints-registry.md`

### Step 3.1 — Add to workflow.md Rule 3 task completion gates

In `workflow.md` Rule 3, the "Task completion verification gates" section already has:
- Demo statement verification
- DI registration check  
- Acceptance criteria check

Add a new gate **"Solution Item Registration check"** after the DI registration check:

```markdown
**4. Solution item registration check**
If the task created any new file that should be visible in VS Solution Explorer (markdown docs under `Docs/`, config files at solution root, scripts, `.claude/` files referenced in BACKLOG), confirm it is registered in `MyVocaList.sln` under the appropriate Solution Folder. An unregistered file compiles and works but is invisible in VS IDE — Helder cannot see or navigate to it.
```

### Step 3.2 — Add to constraints-registry.md

Add a new section at the bottom:

```markdown
---

## Visual Studio Solution (.sln)

- **Solution item registration:** Any file that should be visible in VS Solution Explorer must be listed in `MyVocaList.sln` under the appropriate Solution Folder (`ProjectSection(SolutionItems) = preProject`). Pattern: `RelativePath\file.md = RelativePath\file.md`. Missing entries do not cause build failures but make files invisible in VS. Add as part of the task that creates the file — not as a follow-up.
```

### Step 3.3 — Commit
```
amend: add VS solution file registration rule — workflow.md task gate + constraints-registry
```

---

## Phase 4 — Proactive BACKLOG Entry Rule

**File touched:** `.claude/rules/workflow.md`

### Step 4.1 — Add new subsection to Rule 1

In `workflow.md` Rule 1, after the "New feature workflow" numbered steps (0–5), add a new subsection:

```markdown
### Proactive BACKLOG triage — Untracked work

**Any work identified during a session that is not already in BACKLOG.md must get a brief entry before proceeding.**

This applies to:
- A new DevCycleCraft activity (tooling change, process rule, infrastructure work)
- A business feature idea mentioned in conversation (even informally)
- A significant constraint, investigation, or one-off fix that took material effort

**Format — add a row to the appropriate BACKLOG.md table:**

| Date | Activity/Feature | `💡 Pending` | One-line description |

- Use `💡 Pending` for ideas that arrived but aren't being acted on immediately
- Use `🟡 In Progress` if work is starting now
- Keep descriptions to one sentence — BACKLOG is a dashboard, not a spec

**Trigger questions** (ask at any point in a session):
- "Is what I'm about to do tracked in BACKLOG.md?"
- "Did Helder mention a feature or idea that has no BACKLOG row?"
- "Did I discover a process gap that warrants a DevCycleCraft entry?"

If the answer is "no" to the first, or "yes" to the others → add the entry, then proceed.
```

### Step 4.2 — Commit
```
amend: add proactive BACKLOG triage rule to workflow.md Rule 1
```

---

## Phase 5 — Update BACKLOG status for completed activities

Once Phases 3 and 4 are done, update the two new BACKLOG rows from `🟡 In Progress` → `✅ Done`.

### Step 5.1 — Commit
```
chore: mark VS solution registration + proactive BACKLOG rules as Done in BACKLOG.md
```

---

## Verification

After all phases:
1. Open VS — confirm `findings.md` appears under the `workflow-folder-layout-alignment` solution folder in Solution Explorer.
2. Grep BACKLOG.md for `Docs/DevEnv/workflow-layout-findings.md` — should return no results.
3. Confirm new BACKLOG rows are present and marked `✅ Done`.
4. Confirm write protection is restored in `settings.json` (check `permissions.deny` list).
5. Run `git log --oneline -5` to verify all 5 commits landed cleanly.

---

## Notes

- **Ceremony level:** All phases are Minimal (docs/rules updates, < 30 min each). No spec files required.
- **Who executes:** Main agent handles git-move and .sln edit (shell steps). Rules edits are also main agent since workflow.md and constraints-registry.md are `.claude/rules/` files requiring write-protection removal.
- **findings.md content is unchanged** — only its location changes. No content edits needed.
