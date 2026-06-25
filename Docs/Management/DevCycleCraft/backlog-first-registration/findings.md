# Spike Findings — BACKLOG-first Registration Enforcement

Date: 2026-06-24 · Time-box: 60 min (completed inline ~15 min) · Spike gate: AC-8

> Run inline by the orchestrator after two consecutive subagent infrastructure
> failures (session-limit, then mid-stream stall). The spike is read-only shell
> investigation plus this single markdown deliverable — it touches no `.cs`/`.xaml`
> source and writes no `.py`, so it is within orchestrator read/write scope.

---

## Q1 — Path determinism (PRIMARY)

**Verdict: DETERMINISTIC.**

The device-scoped auto-memory directory is keyed to the **main repo path**, not the
worktree path. A Stop hook running inside a worktree session that naively mangles
`cwd` or `$CLAUDE_PROJECT_DIR` would compute the WRONG directory
(`C--Users-helde-source-repos-MyVocaList--claude-worktrees-backlog-first-registration`,
which holds the worktree session transcript — not project memory). `$CLAUDE_PROJECT_DIR`
was also observed **unset** in this session, so it cannot be relied on at all.

The trap is avoided because git itself resolves a worktree back to the main repo.

**Resolution recipe (what `orphan_check.py` must use):**

1. `git rev-parse --git-common-dir` → from a worktree this returns the **main** repo's
   `.git` path (`C:/Users/helde/source/repos/MyVocaList/.git`), not the worktree's
   `.git/worktrees/<name>` dir. In the main repo it returns `.git` directly. Either way,
   stripping a trailing `/.git` yields the canonical main repo root.
2. Strip the trailing `/.git` → main root `C:/Users/helde/source/repos/MyVocaList`.
3. Mangle: replace every `:`, `/`, and `\` with `-` (regex `[:/\\]` → `-`) →
   `C--Users-helde-source-repos-MyVocaList`.
4. Memory dir = `~/.claude/projects/<mangled>/memory/`
   (`~` = the user home; resolve via `os.path.expanduser("~")` / `Path.home()`).

**Evidence (commands run + outputs):**

```
$ git rev-parse --git-common-dir
C:/Users/helde/source/repos/MyVocaList/.git          # worktree → MAIN .git ✓

$ git rev-parse --git-dir
C:/Users/helde/source/repos/MyVocaList/.git/worktrees/backlog-first-registration  # the trap path

$ echo "$CLAUDE_PROJECT_DIR"
<unset>                                               # do NOT depend on this

# Derivation chain, fully scripted:
Derived main root:  C:/Users/helde/source/repos/MyVocaList
Mangled name:       C--Users-helde-source-repos-MyVocaList
Computed memory dir: C:/Users/helde/.claude/projects/C--Users-helde-source-repos-MyVocaList/memory
projects/  EXISTS
memory/    EXISTS
MEMORY.md  EXISTS                                     # computed path matches reality ✓
```

The computed mangled name matches the real on-disk directory exactly, and the
`memory/` dir + `MEMORY.md` resolve from the worktree. Determinism confirmed.

> Robustness fallback: if `git rev-parse` ever fails (e.g. hook run outside a repo),
> `orphan_check.py` must fail open (skip the check, `return 0`) per the advisory posture —
> never guess a path.

---

## Q2 — Hook observability (SECONDARY)

**Verdict: OBSERVABLE.**

A memory-file write IS observed by an existing hook during the session. The PostToolUse
`Edit|Write` hook group appends every changed file path to `.claude/changed-files.txt`,
and memory-dir writes are present in that log.

**Evidence:**

```
$ grep -c "projects/.*memory" .claude/changed-files.txt
29                                                    # 29 memory-write lines logged

$ grep "memory" .claude/changed-files.txt | tail -5
../../../.claude/projects/C--Users-helde-source-repos-MyVocaList/memory/project_song_import_resolution.md
../../../.claude/projects/C--Users-helde-source-repos-MyVocaList/memory/project_artists_songs_roadmap.md
../../../.claude/projects/C--Users-helde-source-repos-MyVocaList/memory/MEMORY.md
../../../.claude/projects/C--Users-helde-source-repos-MyVocaList/memory/project_settings_local_tracked.md
../../../.claude/projects/C--Users-helde-source-repos-MyVocaList/memory/MEMORY.md
```

**Nuance worth encoding:** the logged paths carry a relative prefix
(`../../../.claude/projects/...`) because the hook records paths relative to cwd. So the
detector must match the **substring** `projects/<mangled>/memory/` (or simply
`/memory/<file>.md` under the project mangle), NOT an absolute-path equality. A substring
match is robust across both worktree and main-repo cwds.

---

## Option B (PostToolUse memory-write buffer) verdict

**VIABLE.** Both gates pass: the memory dir is deterministically resolvable (Q1) and
memory writes are observable mid-session via the existing `changed-files.txt` log (Q2).
No new top-level settings key is required — the PostToolUse `Edit|Write` group already
captures the writes; an orphan check can read `changed-files.txt` (filtered to this
session) at Stop time rather than maintaining its own buffer.

This means the advisory does **not** have to fall back to reviewer-driven-only. It can
deterministically: (a) detect candidate memory writes this session, (b) check whether
BACKLOG.md changed this session, and (c) warn if a candidate exists with no BACKLOG change.

> Even though Option B is viable, the implementation stays **fail-open and advisory**
> (always `return 0`, `2>/dev/null || true`). No mtime baseline is needed — observability
> via the existing log supersedes it.

---

## Proposed `design.md §4` delta — DO NOT APPLY (orchestrator applies it)

Replace the §4 spike-outcome placeholder with:

> **§4 Spike outcome (resolved 2026-06-24 — AC-8 cleared).**
> Both spike questions passed. **Path determinism: DETERMINISTIC** — `orphan_check.py`
> resolves the project memory dir via `git rev-parse --git-common-dir` → strip `/.git` →
> mangle `[:/\\]→-` → `~/.claude/projects/<mangled>/memory/`. It must NOT use cwd or
> `$CLAUDE_PROJECT_DIR` (the latter was unset; the former yields the worktree-mangled trap
> path). On any `git rev-parse` failure it fails open (`return 0`). **Hook observability:
> OBSERVABLE** — the existing PostToolUse `Edit|Write` group already logs memory-dir writes
> to `.claude/changed-files.txt` (verified: 29 logged lines), matched by the substring
> `projects/<mangled>/memory/`. **Option B is VIABLE**: the Stop check reads the
> session-scoped `changed-files.txt` for candidate memory writes — no separate buffer and
> no mtime baseline required. Posture remains advisory/fail-open: warn only, always exit 0.
