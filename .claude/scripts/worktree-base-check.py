#!/usr/bin/env python
"""PostToolUse check: every git worktree must be based on develop.

Fires after EnterWorktree / `git worktree add`. Rather than parsing tool
input (native tool output shape is not guaranteed), it enumerates ALL
worktrees and verifies `develop` is an ancestor of each worktree's HEAD.
The native EnterWorktree tool may base worktrees on the repo default
branch (main), which is stale — workflow.md Rule 2 requires develop.

Warn-only (exit 0 with message on stdout): PostToolUse cannot undo the
creation; the agent must recreate the worktree from develop.
Fail-open on any error.
"""
import os
import subprocess
import sys


def run(args, cwd):
    return subprocess.run(args, cwd=cwd, capture_output=True, text=True, timeout=15)


def main() -> int:
    try:
        proj = os.environ.get("CLAUDE_PROJECT_DIR", ".")
        r = run(["git", "worktree", "list", "--porcelain"], proj)
        if r.returncode != 0:
            return 0
        paths = [ln[len("worktree "):] for ln in r.stdout.splitlines()
                 if ln.startswith("worktree ")]
        main_tree = paths[0] if paths else None
        bad = []
        for p in paths:
            if p == main_tree:
                continue  # main working tree stays on develop by design
            chk = run(["git", "-C", p, "merge-base", "--is-ancestor",
                       "develop", "HEAD"], proj)
            if chk.returncode != 0:
                br = run(["git", "-C", p, "branch", "--show-current"], proj)
                bad.append((p, br.stdout.strip() or "detached"))
        if bad:
            for p, br in bad:
                print(f"⚠️ WORKTREE BASE VIOLATION — {p} (branch `{br}`) "
                      f"is NOT based on develop (likely based on stale main).")
            print("Fix before dispatching work: remove the worktree and recreate "
                  "from develop — `git worktree remove <path>` then "
                  "`git worktree add <path> -b <branch> develop` "
                  "(workflow.md Rule 2 [HARD RULE]).")
    except Exception:
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main())
