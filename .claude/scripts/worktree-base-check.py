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

BEHIND (NOT a violation, never reported)
    Correctly forked from develop but N commits behind because develop has
    advanced since. This is the normal state of any worktree older than a
    day, so the hook stays silent about it.

Warn-only (exit 0 always). A MISBASED finding is returned as JSON
additionalContext so the model sees it. Fail-open on any error.
No repo mutation.
"""
import json
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
    # A branch already merged into develop has no work of its own left; its fork point
    # is also in main after a release merge, which used to read as "misbased".
    if _ok(["git", "-C", path, "merge-base", "--is-ancestor", "HEAD", "develop"], proj):
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

        # BEHIND is the normal state of an older worktree: not reported.
        if misbased:
            lines = [f"WORKTREE BASE VIOLATION - {p} (branch `{br}`) was forked "
                     f"from main's history, not develop (workflow.md Rule 2 "
                     f"[HARD RULE])." for p, br, _n in misbased]
            lines.append("Rebase onto develop, or -- only if the branch has no commits "
                         "you need -- recreate it with "
                         "`git worktree add <path> -b <branch> develop`. "
                         "Check `git log develop..HEAD` before discarding anything.")
            # JSON additionalContext: plain stdout from a tool hook does not reach the model.
            print(json.dumps({"hookSpecificOutput": {
                "hookEventName": "PostToolUse",
                "additionalContext": "\n".join(lines)}}))
    except Exception:
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main())
