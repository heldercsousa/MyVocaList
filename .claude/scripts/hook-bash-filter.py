#!/usr/bin/env python
"""Run a hook command only when the Bash/PowerShell command matches a regex.

Hook matchers match the tool NAME only, so `Bash(git worktree add*)` never fires.
Usage (hook command):  python hook-bash-filter.py "<regex>" -- <cmd> [args...]
The hook JSON payload is forwarded on stdin. Fail-open: always exits 0.
"""
import json
import re
import subprocess
import sys


def main():
    if "--" not in sys.argv:
        return
    sep = sys.argv.index("--")
    pattern, cmd = sys.argv[1], sys.argv[sep + 1:]
    raw = sys.stdin.read()
    try:
        command = (json.loads(raw).get("tool_input") or {}).get("command", "")
    except (ValueError, AttributeError):
        return
    if cmd and re.search(pattern, command or ""):
        subprocess.run(cmd, input=raw, text=True, timeout=120)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        pass
    sys.exit(0)
