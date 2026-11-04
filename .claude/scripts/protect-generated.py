#!/usr/bin/env python
"""PreToolUse guard: block Edit/Write on generated files and secrets.

Blocked: EF migration designers + model snapshot (regenerate with `dotnet ef`),
`.env*` files (except `*.example`), DevExpress license files.
Exit 2 + stderr blocks the tool call; any error fails open (exit 0).
"""
import json
import os
import re
import sys

RULES = [
    (re.compile(r"[\\/]Migrations[\\/][^\\/]+\.Designer\.cs$", re.I),
     "EF migration designer files are generated - use `dotnet ef migrations`."),
    (re.compile(r"ModelSnapshot\.cs$", re.I),
     "The EF model snapshot is generated - use `dotnet ef migrations`."),
    (re.compile(r"(^|[\\/])\.env(\.[^\\/]*)?$", re.I),
     ".env files hold secrets - edit them by hand."),
    (re.compile(r"DevExpress_License\.txt$", re.I),
     "DevExpress license files hold secrets - edit them by hand."),
]


def blocked_reason(path):
    if not path or path.lower().endswith(".example"):
        return None
    for rx, reason in RULES:
        if rx.search(path):
            return reason
    return None


def main():
    try:
        payload = json.load(sys.stdin)
        path = (payload.get("tool_input") or {}).get("file_path", "")
    except Exception:
        return 0
    reason = blocked_reason(path)
    if reason:
        sys.stderr.write(f"BLOCKED ({os.path.basename(path)}): {reason}\n")
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
