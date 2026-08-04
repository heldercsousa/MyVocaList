#!/usr/bin/env python
"""Worktree base check: every git worktree must be FORKED FROM develop.

Fires after worktree creation (PostToolUse on EnterWorktree / `git worktree
add`, and TaskCreated for Agent-tool `isolation: worktree` dispatches).
It enumerates ALL worktrees and classifies each one.

Two distinct conditions are reported:

MISBASED (real workflow.md Rule 2 violation)
    The branch forked from *main's* history rather than develop's. Predicate:
    let MB = merge-base(develop, HEAD). MB is the fork point. If MB is
    contained in main's history while develop has commits main does not have,
    the branch was cut from main (or an ancestor), never from a develop-only
    commit -- so it is misbased. If MB is a develop-only commit, `MB in main`
    is false and the branch is correctly based.

BEHIND (advisory only, NOT a violation)
    Correctly forked from develop but N commits behind because develop has
    advanced since. This is the normal state of any worktree older than a
    day. Never suggest removing/recreating such a worktree -- it may hold
    unmerged commits, and deleting it is data loss.

Warn-only (exit 0 always, message on stdout). Fail-open on any error.
No repo mutation.
"""
import os
import subprocess
import sys


def run(args, cwd):
    return subprocess.run(args, cwd=cwd, capture_output=True, text=True, timeout=15)


def _ok(args, cwd):
    return run(args, cwd).returncode == 0


def _count(args, cwd):
    r = run(args, cwd)
    try:
        return int(r.stdout.strip())
    except (ValueError, AttributeError):
        return None


def classify(proj, path):
    """Return (state, detail) where state is 'clean' | 'behind' | 'misbased'."""
    if _ok(["git", "-C", path, "merge-base", "--is-ancestor", "develop", "HEAD"], proj):
        return ("clean", 0)

    mb = run(["git", "-C", path, "merge-base", "develop", "HEAD"], proj)
    if mb.returncode != 0 or not mb.stdout.strip():
        return ("clean", 0)  # cannot determine -- fail open
    fork = mb.stdout.strip()

    behind = _count(["git", "-C", path, "rev-list", "--count",
                     f"{fork}..develop"], proj)
    if behind is None:
        behind = 0

    # Only discriminate against main when main exists AND develop has
    # commits main lacks; otherwise "MB in main" carries no information.
    has_main = _ok(["git", "-C", path, "rev-parse", "--verify",
                    "--quiet", "main"], proj)
    if has_main and not _ok(["git", "-C", path, "merge-base", "--is-ancestor",
                             "develop", "main"], proj):
        if _ok(["git", "-C", path, "merge-base", "--is-ancestor", fork, "main"], proj):
            return ("misbased", behind)
    return ("behind", behind)


def main() -> int:
    try:
        proj = os.environ.get("CLAUDE_PROJECT_DIR", ".")
        r = run(["git", "worktree", "list", "--porcelain"], proj)
        if r.returncode != 0:
            return 0
        paths = [ln[len("worktree "):] for ln in r.stdout.splitlines()
                 if ln.startswith("worktree ")]
        main_tree = paths[0] if paths else None
        misbased, behind = [], []
        for p in paths:
            if p == main_tree:
                continue  # main working tree stays on develop by design
            state, n = classify(proj, p)
            if state == "clean":
                continue
            br = run(["git", "-C", p, "branch", "--show-current"], proj)
            entry = (p, br.stdout.strip() or "detached", n)
            (misbased if state == "misbased" else behind).append(entry)

        for p, br, _n in misbased:
            print(f"WORKTREE BASE VIOLATION - {p} (branch `{br}`) was forked "
                  f"from main's history, not develop (workflow.md Rule 2 "
                  f"[HARD RULE]).")
        if misbased:
            print("Rebase onto develop, or -- only if the branch has no commits "
                  "you need -- recreate it with "
                  "`git worktree add <path> -b <branch> develop`. "
                  "Check `git log develop..HEAD` before discarding anything.")
        for p, br, n in behind:
            print(f"note: {p} (branch `{br}`) is correctly based on develop but "
                  f"is {n} commit(s) behind it. Not a violation. To catch up: "
                  f"`git merge --ff-only develop` (or rebase onto develop).")
    except Exception:
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main())
