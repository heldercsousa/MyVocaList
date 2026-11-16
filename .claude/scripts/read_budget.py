"""Read budgets shared by the read guards (`read-slice-guard.py`, `bash-guard.py`).

A file within BOTH full-read budgets may be read whole. Over either budget it must be read in
slices: an explicit `limit` of at most MAX_SLICE_LINES lines, and at most MAX_SLICE_BYTES bytes.
Lines alone are not enough: `Docs/Management/BACKLOG.md` is ~140 lines and ~35 KB.
"""
import os
import re

MAX_FULL_LINES = 200
MAX_FULL_BYTES = 12_000
MAX_SLICE_LINES = 250
MAX_SLICE_BYTES = 20_000

EXEMPT_SEGMENTS = (".claude/", "CLAUDE.md", "Directory.Build.props", "GlobalUsings.cs")
EXEMPT_SUFFIXES = (".csproj", ".ipynb", ".pdf")
BACKLOG_SUFFIX = "Docs/Management/BACKLOG.md"
BACKLOG_HINT = (
    "BACKLOG.md is a generated human view. Query it instead: "
    'python .claude/scripts/backlog/backlog_gen.py query --status "🟡,🟢"'
)


def norm(path: str) -> str:
    return path.replace("\\", "/")


_WORKTREE = re.compile(r"(?:^|/)\.claude/worktrees/[^/]+/")


def is_exempt(path: str) -> bool:
    """Exempt paths are judged relative to their checkout.

    A worktree lives under `.claude/worktrees/<name>/`; that prefix must not make every source
    file in it exempt, so it is stripped first. The worktree's own `.claude/` stays exempt.
    """
    p = norm(path)
    m = None
    for m in _WORKTREE.finditer(p):
        pass
    if m:
        p = p[m.end():]
    return any(seg in p for seg in EXEMPT_SEGMENTS) or p.lower().endswith(EXEMPT_SUFFIXES)


def is_binary(path: str) -> bool:
    try:
        with open(path, "rb") as f:
            return b"\0" in f.read(8192)
    except OSError:
        return True


def read_lines(path: str):
    """The file as a list of byte lines, or None when it cannot be read."""
    try:
        with open(path, "rb") as f:
            return f.read().splitlines(keepends=True)
    except OSError:
        return None


def over_full_budget(line_count: int, size: int) -> bool:
    return line_count > MAX_FULL_LINES or size > MAX_FULL_BYTES


def _as_int(value):
    try:
        return int(value)
    except (TypeError, ValueError):
        return None


def describe(line_count: int, size: int) -> str:
    return f"{line_count} lines, {size / 1000:.1f} KB"


def decide(path: str, offset=None, limit=None):
    """Reason to deny this read, or None to allow it."""
    if not path or not os.path.isfile(path) or is_exempt(path) or is_binary(path):
        return None
    lines = read_lines(path)
    if lines is None:
        return None
    size = sum(len(ln) for ln in lines)
    if not over_full_budget(len(lines), size):
        return None

    what = describe(len(lines), size)
    hint = " " + BACKLOG_HINT if norm(path).endswith(BACKLOG_SUFFIX) else ""
    limit = _as_int(limit)
    if limit is None:
        return (
            f"File is {what} (full-read budget: {MAX_FULL_LINES} lines and "
            f"{MAX_FULL_BYTES // 1000} KB). Use Grep to locate the target section, then call Read "
            f"with explicit `offset` and `limit` (`limit` <= {MAX_SLICE_LINES} lines per slice)."
            + hint
        )
    if limit > MAX_SLICE_LINES:
        return (
            f"`limit` {limit} exceeds the slice cap of {MAX_SLICE_LINES} lines (file is {what}). "
            "Use Grep to locate the target section and read a smaller slice." + hint
        )
    start = max((_as_int(offset) or 1) - 1, 0)
    slice_bytes = sum(len(ln) for ln in lines[start:start + limit])
    if slice_bytes > MAX_SLICE_BYTES:
        return (
            f"This slice is {slice_bytes / 1000:.1f} KB (slice budget: {MAX_SLICE_BYTES // 1000} KB; "
            f"file is {what}, so its lines are long). Lower `limit`, or use Grep." + hint
        )
    return None
