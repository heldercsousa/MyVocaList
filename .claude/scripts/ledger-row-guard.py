"""Ledger row-length guard.

Docs/Management/LEDGER.md is read at every session start, so each table row must stay
one short status line (see `/sln-ledger § Row-length rule`). Detail belongs in the item's
task-log.md / README.md.

Modes:
  python ledger-row-guard.py --check    -> CLI / pre-commit: exit 1 when any row is too long
  python ledger-row-guard.py (stdin)    -> PostToolUse hook: warn-only when the edited file is LEDGER.md
"""
import json
import os
import sys

LIMIT = 450
LEDGER = os.path.join("Docs", "Management", "LEDGER.md")


def long_rows(path):
    bad = []
    with open(path, encoding="utf-8", errors="ignore") as f:
        for n, line in enumerate(f, 1):
            line = line.rstrip("\n")
            if line.startswith("| ") and not line.startswith("| ID") and len(line) > LIMIT:
                bad.append((n, line.split("|")[1].strip(), len(line)))
    return bad


def report(bad):
    return "\n".join(
        f"  LEDGER.md line {n}: row '{i}' is {c} chars (limit {LIMIT}). "
        "Move detail to the item's task-log.md/README.md; keep one status line."
        for n, i, c in bad
    )


def main():
    if "--check" not in sys.argv:
        try:
            fp = json.load(sys.stdin).get("tool_input", {}).get("file_path", "")
        except Exception:
            return 0
        if not fp.replace(os.sep, "/").endswith("Docs/Management/LEDGER.md") or not os.path.isfile(fp):
            return 0
        bad = long_rows(fp)
        if bad:
            print("LEDGER ROW-LENGTH WARNING:\n" + report(bad))
        return 0
    root = os.environ.get("CLAUDE_PROJECT_DIR", ".")
    path = os.path.join(root, LEDGER)
    if not os.path.isfile(path):
        return 0
    bad = long_rows(path)
    if bad:
        print("ledger-row-guard: FAILED\n" + report(bad))
        return 1
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception:
        sys.exit(0)
