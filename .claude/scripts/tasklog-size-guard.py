import json
import os
import sys

MAX_TASKLOG_LINES = 300


def main() -> None:
    try:
        payload = json.load(sys.stdin)
    except Exception:
        sys.exit(0)

    fp = (payload.get("tool_input") or {}).get("file_path") or ""
    normalized = fp.replace("\\", "/")
    if not normalized.endswith("/task-log.md") and normalized != "task-log.md":
        sys.exit(0)
    if not os.path.isfile(fp):
        sys.exit(0)

    with open(fp, encoding="utf-8", errors="ignore") as f:
        lines = sum(1 for _ in f)

    if lines > MAX_TASKLOG_LINES:
        folder = os.path.dirname(normalized) or "."
        print(
            f"TASK-LOG ROTATION REQUIRED: `{normalized}` is {lines} lines (cap: {MAX_TASKLOG_LINES}). "
            f"Before closing this task, move closed/merged entries into `{folder}/task-log-archive-YYYY-MM.md`, "
            "keeping only the header and active/open entries."
        )


if __name__ == "__main__":
    main()