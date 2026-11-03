#!/usr/bin/env python3
"""PreToolUse (Write|Edit) reminder: code-file edit by the main agent.

Silent when an ITF declaration (`.itf-active`) exists in the edited file's
worktree — the edit is then inside the declared lane and Guard 3
(constitutional-guard.py) enforces its bounds. Otherwise prints a one-line
reminder naming both legitimate routes. Never blocks (always exits 0).

Spec: Docs/Management/DevCycleCraft/inline-trivial-fix/changes/
      2026-08-18-recalibrate-agent-dispatch-settings-.../requirements.md AC-ADR-07
"""
import json
import os
import sys

_CODE_SUFFIXES = (".cs", ".xaml", ".xaml.cs")


def _has_declaration(file_path: str) -> bool:
    d = os.path.dirname(os.path.abspath(file_path))
    while True:
        if os.path.isfile(os.path.join(d, ".itf-active")):
            return True
        if os.path.exists(os.path.join(d, ".git")):
            return False  # reached the worktree root without a marker
        parent = os.path.dirname(d)
        if parent == d:
            return False
        d = parent


def main() -> int:
    try:
        data = json.load(sys.stdin)
        fp = data.get("tool_input", {}).get("file_path", "")
        if fp.endswith(_CODE_SUFFIXES) and not _has_declaration(fp):
            print("WORKFLOW REMINDER: main agent is editing a code file with no ITF "
                  "declaration. For a diagnosed 1-file / <=5-line fix run /sln-itf; "
                  "otherwise dispatch an implementor (workflow.md Rule 2).")
    except Exception:
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main())
