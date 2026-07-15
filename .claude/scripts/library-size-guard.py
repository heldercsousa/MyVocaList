"""PostToolUse guard: warn when a .claude/library file regrows past the section-size budget.

Rationale (2026-07-14 token-scoped reads amendment): large library files were split
into <=~225-line section files so subagents read only what a task needs. This guard
keeps the split honest — when an edited library file exceeds LIMIT lines, it prints
a warning telling the agent to split the file instead of letting it regrow.
Warn-only; never blocks.
"""
import json
import os
import sys

LIMIT = 400

try:
    data = json.load(sys.stdin)
    fp = data.get("tool_input", {}).get("file_path", "")
    norm = fp.replace(os.sep, "/")
    if "/.claude/library/" in norm and norm.endswith(".md") and os.path.isfile(fp):
        with open(fp, encoding="utf-8", errors="ignore") as f:
            count = sum(1 for _ in f)
        if count > LIMIT:
            print(
                f"LIBRARY SIZE BUDGET WARNING: {os.path.basename(fp)} is {count} lines "
                f"(budget {LIMIT}). Split it into section files (see the 2026-07-14 "
                "token-scoped-reads pattern: family index stub + `<prefix>-<topic>.md` "
                "section files) instead of letting it regrow."
            )
except Exception:
    pass
