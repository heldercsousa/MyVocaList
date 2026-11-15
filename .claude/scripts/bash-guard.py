"""PreToolUse (Bash|PowerShell) guard. Two checks, both fail-open.

1. Worktree location (DENY): `git worktree add <path>` is refused unless <path> resolves under
   `<main checkout>/.claude/worktrees/`. That is where Claude Code itself creates worktrees
   (EnterWorktree, --worktree, agent isolation), so the project has one location.
2. Large reads (NOTICE, never blocks): a plain `cat` / `head` / `tail` / `sed -n` / `type` /
   `Get-Content` on a file over the read budget (`read_budget.py`) returns a one-line note to the
   model. Segments with a pipe or a redirect are skipped: their output size cannot be judged.
"""
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import hook_out  # noqa: E402
import read_budget  # noqa: E402

WORKTREE_DIR = ".claude/worktrees"
_TOKEN = re.compile(r'"([^"]*)"|\'([^\']*)\'|(\S+)')
_HEREDOC = re.compile(r"<<-?\s*(['\"]?)(\w+)\1.*?^\s*\2\s*$", re.S | re.M)
_WT_ADD = re.compile(r"^\s*(?:rtk\s+(?:proxy\s+)?)?git\s+((?:-C\s+(?:\"[^\"]*\"|'[^']*'|\S+)\s+)?)worktree\s+add\b(.*)", re.S)
_VALUE_FLAGS = {"-b", "-B", "--reason"}
_READERS = {"cat", "head", "tail", "sed", "type", "get-content", "gc"}
_COUNT_FLAGS = {"-n", "-totalcount", "-tail", "-first", "-head", "-last"}
_DYNAMIC = re.compile(r"[$`%*?]")


def tokens(text: str):
    return [a or b or c for a, b, c in _TOKEN.findall(text)]


def _segments(command: str):
    """Command split into simple segments. A heredoc body is data, not a command."""
    command = _HEREDOC.sub("<<", command)
    return [s for s in re.split(r"&&|\|\||;|\n", command) if s.strip()]


def _resolve(path: str, base: str) -> str:
    path = os.path.expanduser(path)
    if not os.path.isabs(path):
        path = os.path.join(base, path)
    return os.path.normcase(os.path.realpath(path)).replace("\\", "/")


def worktree_violation(command: str, cwd: str, project: str):
    """Deny reason when the command adds a worktree outside WORKTREE_DIR, else None."""
    allowed = _resolve(os.path.join(project, *WORKTREE_DIR.split("/")), project) + "/"
    for seg in _segments(command):
        m = _WT_ADD.match(seg)
        if not m:
            continue
        base = cwd
        c_arg = tokens(m.group(1))
        if len(c_arg) == 2:
            base = _resolve(c_arg[1], cwd)
        args = tokens(re.split(r"\||>", m.group(2))[0])
        target, skip = None, False
        for a in args:
            if skip:
                skip = False
            elif a in _VALUE_FLAGS:
                skip = True
            elif not a.startswith("-"):
                target = a
                break
        if not target or _DYNAMIC.search(target):
            continue  # cannot judge -> fail open
        if not (_resolve(target, base) + "/").startswith(allowed):
            return (
                f"Worktrees live only under `{WORKTREE_DIR}/` in the main checkout "
                f"(`{target}` resolves outside it). Use: "
                f"git worktree add \"{project.replace(chr(92), '/')}/{WORKTREE_DIR}/<name>\" "
                "-b <branch> develop  (or the EnterWorktree tool)."
            )
    return None


def _line_request(name: str, args):
    """(limit, files) for one reader call. limit None = whole file."""
    limit, files, skip = None, [], False
    if name in ("head", "tail"):
        limit = 10
    for i, a in enumerate(args):
        low = a.lower()
        if skip:
            skip = False
            continue
        if low in _COUNT_FLAGS and i + 1 < len(args) and args[i + 1].lstrip("+").isdigit():
            if low == "-n" and name == "sed":
                continue
            limit, skip = int(args[i + 1].lstrip("+")), True
        elif re.fullmatch(r"-\d+", a):
            limit = int(a[1:])
        elif name == "sed" and re.fullmatch(r"(\d+),(\d+)p", a):
            lo, hi = map(int, a[:-1].split(","))
            limit = max(hi - lo + 1, 0)
        elif name == "sed" and re.fullmatch(r"\d+p", a):
            limit = 1
        elif not a.startswith("-"):
            files.append(a)
    return limit, files


def read_notices(command: str, cwd: str):
    out = []
    for seg in _segments(command):
        if "|" in seg or ">" in seg or "<" in seg:
            continue
        toks = tokens(seg)
        while toks and toks[0] in ("rtk", "proxy"):
            toks = toks[1:]
        if not toks or toks[0].lower() not in _READERS:
            continue
        name = toks[0].lower()
        limit, files = _line_request(name, toks[1:])
        for f in files:
            if _DYNAMIC.search(f):
                continue
            path = f if os.path.isabs(f) else os.path.join(cwd, f)
            reason = read_budget.decide(path, None, limit)
            if reason:
                out.append(
                    f"READ BUDGET: `{toks[0]}` on `{f}` bypasses the read guard. {reason}"
                )
    return out


def main() -> None:
    try:
        payload = json.load(sys.stdin)
        command = (payload.get("tool_input") or {}).get("command") or ""
        cwd = payload.get("cwd") or os.getcwd()
        project = os.environ.get("CLAUDE_PROJECT_DIR") or cwd
        if not command:
            return
        reason = worktree_violation(command, cwd, project)
        if reason:
            hook_out.deny(reason)
            return
        notices = read_notices(command, cwd)
        if notices:
            hook_out.additional_context("PreToolUse", "\n".join(notices))
    except Exception:
        return


if __name__ == "__main__":
    main()
