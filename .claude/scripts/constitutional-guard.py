#!/usr/bin/env python
"""PreToolUse blocking guard for constitutional + workflow HARD RULEs.

Guard 1 (branch guard, added 2026-07-14): blocks a Write/Edit to a
.cs / .xaml / .xaml.cs file while the file's checkout is on develop or main.
Code work happens in git worktrees on task branches (workflow.md Rule 2);
develop is the docs + integration branch. Worktree edits pass automatically
(their checkout is on a task branch).

Guard 2: blocks a Write/Edit that would INTRODUCE a native-dialog call into a
.cs / .xaml / .xaml.cs file. Native dialogs (DisplayAlert / DisplayActionSheet
/ DisplayPromptAsync) are forbidden by CLAUDE.md
(Constitutional Constraints -> Native dialogs, [Unamendable]). Use dx:BottomSheet.

Guard 3 (ITF bounds, added 2026-07-21): enforces the Inline Trivial Fix lane's
mechanical conditions (C1 / C3 / C4 / C5 -- workflow.md Rule 2 "Inline Trivial
Fix (ITF) lane"). It is INERT unless a declaration marker `.itf-active` exists
at the root of the worktree containing the edited file. Without a marker the
lane is not entered and no ITF bound applies to anyone -- which is what keeps
implementor subagents unconstrained (AC-ITF-10). Full spec:
Docs/Management/DevCycleCraft/inline-trivial-fix/.

Design notes:
- Only the NEW text is scanned (Write.content or Edit.new_string), so the guard
  fires only when a call is being added -- not when an unrelated edit touches a
  file that already (legitimately) mentions the term in a comment.
- Match requires the call form `Name(` (optional whitespace before the paren) so
  that prose/comments referencing the API by name are not false-positived.
- Fail-open: any internal error exits 0 so the guard can never break the workflow.
- Blocks via exit code 2 (PreToolUse: stderr is surfaced to the agent, tool call
  is denied).
"""
import json
import os
import re
import subprocess
import sys
from datetime import datetime, timedelta, timezone

# Call-form patterns: the API name followed by an opening paren.
_FORBIDDEN = re.compile(r"\bDisplay(?:Alert|ActionSheet|PromptAsync)\s*\(")
_CODE_SUFFIXES = (".cs", ".xaml", ".xaml.cs")
_PROTECTED_BRANCHES = {"develop", "main"}

# ---------------------------------------------------------------------------
# Guard 3 (ITF) constants
# ---------------------------------------------------------------------------
_ITF_MARKER = ".itf-active"
_ITF_MAX_LINES = 5           # C1 -- workflow.md Rule 2, ITF lane
_ITF_EXPIRY = timedelta(minutes=30)   # AC-ITF-11
_ITF_BLOCKED_SUFFIXES = (".xaml", ".xaml.cs")   # C3

# C4 -- governed components. KEEP IN SYNC WITH
# `.claude/library/component-safety-gate.md` (Scope -- what is a "governed
# component"). Matched on the file-name stem, so `ListItem.xaml`,
# `ListItem.xaml.cs` and `ListItem.cs` all match while
# `ListItemLeadingImage.xaml.cs` does not.
_ITF_GOVERNED_COMPONENTS = {
    "SearchAppBar",
    "SmallAppBar",
    "ListItem",
    "EmptyState",
    "AutocompleteField",
    "CrudListView",
}

# C5 -- sequential-only file registry. KEEP IN SYNC WITH
# `.claude/rules/workflow.md` Rule 2 § Sequential-only file registry.
_ITF_SEQUENTIAL_FILES = {
    "MauiProgram.cs",
    "AppShell.xaml",
    "AppShell.xaml.cs",
    "AppDbContext.cs",
    "GlobalUsings.cs",
    "Directory.Build.props",
    "tasks.md",
}


def _branch_guard(file_path: str) -> int:
    """Block code edits when the file's checkout is on develop/main.

    Worktrees carry their own task branch, so edits inside .worktrees/<name>
    pass automatically. Fail-open on any git error (fresh clone, detached
    HEAD during rebase, git missing) so the guard never breaks the workflow.
    """
    try:
        result = subprocess.run(
            ["git", "-C", os.path.dirname(file_path) or ".",
             "branch", "--show-current"],
            capture_output=True, text=True, timeout=10,
        )
        if result.returncode != 0:
            return 0
        branch = result.stdout.strip()
        if branch in _PROTECTED_BRANCHES:
            sys.stderr.write(
                f"BLOCKED — workflow.md Rule 2 [HARD RULE, amended 2026-07-14].\n"
                f"This edit targets a code file while the checkout is on "
                f"`{branch}`:\n  {file_path}\n"
                f"Code edits happen in a git worktree on a task branch — "
                f"never directly on develop/main. develop is the docs + "
                f"integration branch only.\n"
                f"Fix: create/enter a worktree based on develop "
                f"(EnterWorktree, then verify base: "
                f"`git merge-base --is-ancestor develop HEAD`; or "
                f"`git worktree add .worktrees/<name> -b <branch> develop`) "
                f"and make the edit there.\n"
                f"Docs/spec/task-log/BACKLOG edits on develop are allowed "
                f"and unaffected by this guard.\n"
            )
            return 2
    except Exception:
        pass
    return 0


def _worktree_root(file_path: str):
    """Root of the worktree containing file_path, or None on any git error."""
    result = subprocess.run(
        ["git", "-C", os.path.dirname(file_path) or ".",
         "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, timeout=10,
    )
    if result.returncode != 0:
        return None
    root = result.stdout.strip()
    return root.replace("\\", "/") if root else None


def _count_lines(text: str) -> int:
    """Line count of a fragment. Empty string counts as 0."""
    if not text:
        return 0
    return len(text.splitlines()) or 1


def _changed_lines(tool_input: dict) -> int:
    """Upper-bound changed-line count (design.md § Changed-line counting).

    Write     -> lines in `content`
    Edit      -> max(lines(old_string), lines(new_string))
    MultiEdit -> sum over edits
    Upper-bounding is deliberate: it errs toward blocking, and a wrongly
    blocked fix just falls back to dispatching a subagent (the status quo).
    """
    total = 0
    if isinstance(tool_input.get("content"), str):
        total += _count_lines(tool_input["content"])
    if isinstance(tool_input.get("old_string"), str) or \
            isinstance(tool_input.get("new_string"), str):
        total += max(
            _count_lines(tool_input.get("old_string") or ""),
            _count_lines(tool_input.get("new_string") or ""),
        )
    for edit in tool_input.get("edits", []) or []:
        if isinstance(edit, dict):
            total += max(
                _count_lines(edit.get("old_string") or ""),
                _count_lines(edit.get("new_string") or ""),
            )
    return total


def _itf_block(condition: str, file_path: str, detail: str, pointer: str) -> int:
    sys.stderr.write(
        f"BLOCKED — ITF lane bound {condition} "
        f"(workflow.md Rule 2 § Inline Trivial Fix (ITF) lane).\n"
        f"An ITF declaration (`{_ITF_MARKER}`) is active in this worktree, and "
        f"this edit exceeds the bounds it opted into:\n  {file_path}\n"
        f"{detail}\n"
        f"Fix: delete the marker and dispatch an implementor subagent — the ITF "
        f"lane admits no partial qualification. See {pointer}.\n"
    )
    return 2


def _itf_guard(file_path: str, tool_input: dict) -> int:
    """Guard 3 — enforce ITF bounds while a declaration is active.

    Inert (returns 0) when the marker is absent, unparseable, expired, or names
    a file outside this worktree. Fail-open on every exception: a malformed
    declaration is treated as absent, never as authorization (AC-ITF-09).
    """
    try:
        root = _worktree_root(file_path)
        if not root:
            return 0
        marker_path = os.path.join(root, _ITF_MARKER)
        if not os.path.isfile(marker_path):
            return 0  # AC-ITF-10: no declaration -> no ITF bound on any agent

        with open(marker_path, encoding="utf-8") as fh:
            marker = json.load(fh)
        if not isinstance(marker, dict):
            return 0

        # Expiry (AC-ITF-11). Unparseable timestamp -> treated as absent.
        raw = marker.get("declared_at")
        if not isinstance(raw, str):
            return 0
        declared_at = datetime.fromisoformat(raw.replace("Z", "+00:00"))
        if declared_at.tzinfo is None:
            declared_at = declared_at.replace(tzinfo=timezone.utc)
        if datetime.now(timezone.utc) - declared_at > _ITF_EXPIRY:
            return 0

        declared = marker.get("file")
        if not isinstance(declared, str) or not declared:
            return 0
        # `file` is repo-relative with forward slashes (design.md).
        declared_abs = os.path.normpath(
            os.path.join(root, declared.replace("\\", "/"))).replace("\\", "/")
        if not declared_abs.lower().startswith(root.lower()):
            return 0  # declaration does not resolve into this worktree

        # NOTE: marker["expected_lines"] is AUDIT-ONLY and deliberately not read
        # here — the actual count is compared against the constant _ITF_MAX_LINES.

        # Assumption: case-insensitive path comparison. Correct on Windows (the
        # project's dev platform); on a case-sensitive filesystem a declaration
        # for `Foo.cs` would also authorize an edit to `foo.cs` — a different
        # file. Revisit if the project ever builds on Linux/macOS CI.
        target_abs = os.path.normpath(os.path.abspath(file_path)).replace("\\", "/")
        if declared_abs.lower() != target_abs.lower():
            return _itf_block(
                "C1 (exactly 1 file per declaration)", file_path,
                f"The active declaration names a different file:\n  {declared_abs}",
                "`.claude/rules/workflow.md` Rule 2")

        name = os.path.basename(target_abs)
        if name.endswith(_ITF_BLOCKED_SUFFIXES):
            return _itf_block(
                "C3 (no .xaml / .xaml.cs targets)", file_path,
                "XAML edits are categorically outside the ITF lane.",
                "`.claude/rules/workflow.md` Rule 2")

        stem = name.split(".")[0]
        if stem in _ITF_GOVERNED_COMPONENTS:
            return _itf_block(
                "C4 (no governed components)", file_path,
                f"`{stem}` is a governed component; changing it requires the "
                f"four gates (dedicated task + MD3 review, consumer map, "
                f"per-consumer risk assessment, Helder approval).",
                "`.claude/rules/component-change-governance.md`")

        if name in _ITF_SEQUENTIAL_FILES or name.endswith("Migration.cs") \
                or "/Migrations/" in target_abs:
            return _itf_block(
                "C5 (no sequential-only registry files)", file_path,
                f"`{name}` is in the sequential-only file registry.",
                "`.claude/rules/workflow.md` Rule 2 § Sequential-only file registry")

        lines = _changed_lines(tool_input)
        if lines > _ITF_MAX_LINES:
            return _itf_block(
                f"C1 (<= {_ITF_MAX_LINES} changed lines)", file_path,
                f"This edit changes up to {lines} lines "
                f"(upper-bound count), over the {_ITF_MAX_LINES}-line cap.",
                "`.claude/rules/workflow.md` Rule 2")
    except Exception:
        return 0  # fail-open (AC-ITF-09)
    return 0


def main() -> int:
    try:
        data = json.load(sys.stdin)
    except Exception:
        return 0  # fail-open: unreadable payload must not block tools

    tool_input = data.get("tool_input", {}) or {}
    file_path = (tool_input.get("file_path") or "").replace("\\", "/")
    if not file_path.endswith(_CODE_SUFFIXES):
        return 0

    rc = _branch_guard(file_path)
    if rc:
        return rc

    # New text only: Write -> content; Edit -> new_string; MultiEdit -> each edit.
    fragments = []
    if isinstance(tool_input.get("content"), str):
        fragments.append(tool_input["content"])
    if isinstance(tool_input.get("new_string"), str):
        fragments.append(tool_input["new_string"])
    for edit in tool_input.get("edits", []) or []:
        if isinstance(edit, dict) and isinstance(edit.get("new_string"), str):
            fragments.append(edit["new_string"])

    for text in fragments:
        m = _FORBIDDEN.search(text)
        if m:
            call = m.group(0).rstrip("(").strip()
            sys.stderr.write(
                f"BLOCKED — constitutional constraint (CLAUDE.md, [Unamendable]).\n"
                f"This edit introduces `{call}(` into {file_path}.\n"
                f"Native dialogs (DisplayAlert / DisplayActionSheet / DisplayPromptAsync) "
                f"are forbidden — they bypass the app theme, violate MD3 interaction "
                f"patterns, and are not back-gesture dismissible on Android.\n"
                f"Use `dx:BottomSheet` instead — see the `myvocalist-coding` skill "
                f"(dialogs-validation.md). This constraint requires architecture review "
                f"to change; it cannot be relaxed at session level.\n"
            )
            return 2  # deny the tool call

    # Guard 3 runs last: an ITF declaration must never smuggle a develop-branch
    # edit or a native dialog past Guards 1-2.
    return _itf_guard(file_path, tool_input)


if __name__ == "__main__":
    sys.exit(main())
