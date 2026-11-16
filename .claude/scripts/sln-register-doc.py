"""PostToolUse (Write) hook: list a new `Docs/` file in MyVocaList.sln.

The file is placed under the solution folder that mirrors its physical folder (`sln_nest.place`),
so Visual Studio shows the disk tree. Only the main checkout's `Docs/` is handled: a file written
inside a worktree belongs to that worktree's own .sln and is registered when its branch merges.
Fail-open and silent; `process_checks.py` (pre-commit) is the gate that catches a missed file.

Replaces `sync-docs-to-sln.ps1` (2026-11-15), which listed files by a static folder map.
"""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import sln_nest  # noqa: E402

BOM = b"\xef\xbb\xbf"


def rel_docs_path(file_path: str, project: str):
    """`Docs\\...` path relative to the project, or None when the file is not under its Docs/."""
    try:
        rel = os.path.relpath(os.path.abspath(file_path), os.path.abspath(project))
    except ValueError:
        return None
    rel = rel.replace("/", "\\")
    if not rel.startswith("Docs\\") or not os.path.splitext(rel)[1]:
        return None
    return rel


def register(project: str, rel: str) -> bool:
    path = os.path.join(project, "MyVocaList.sln")
    raw = open(path, "rb").read()
    bom = raw.startswith(BOM)
    text = raw[len(BOM):].decode("utf-8") if bom else raw.decode("utf-8")
    if rel in sln_nest.docs_entries(text):
        return False
    new = sln_nest.place(text, [rel])
    open(path, "wb").write((BOM if bom else b"") + new.encode("utf-8"))
    return True


def main() -> int:
    try:
        payload = json.load(sys.stdin)
        fp = (payload.get("tool_input") or {}).get("file_path") or ""
        project = os.environ.get("CLAUDE_PROJECT_DIR") or "."
        if not fp or not os.path.isfile(fp):
            return 0
        rel = rel_docs_path(fp, project)
        if rel:
            register(project, rel)
    except Exception:
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main())
