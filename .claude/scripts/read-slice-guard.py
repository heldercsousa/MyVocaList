"""PreToolUse (Read) guard: large files are read in bounded slices, never whole.

Budgets and the decision live in `read_budget.py` (shared with `bash-guard.py`).
Fail-open: any error allows the read.
"""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import read_budget  # noqa: E402


def main() -> None:
    try:
        payload = json.load(sys.stdin)
        tool_input = payload.get("tool_input") or {}
        reason = read_budget.decide(
            tool_input.get("file_path") or "",
            tool_input.get("offset"),
            tool_input.get("limit"),
        )
    except Exception:
        return
    if reason:
        print(json.dumps({
            "hookSpecificOutput": {
                "hookEventName": "PreToolUse",
                "permissionDecision": "deny",
                "permissionDecisionReason": reason,
            }
        }))


if __name__ == "__main__":
    main()
