import json
import os
import sys

MAX_UNBOUNDED_LINES = 200
EXEMPT_SEGMENTS = (
    ".claude/",
    ".claude\\",
    "CLAUDE.md",
    "Directory.Build.props",
    "GlobalUsings.cs",
)


def main() -> None:
    try:
        payload = json.load(sys.stdin)
    except Exception:
        sys.exit(0)

    tool_input = payload.get("tool_input") or {}
    file_path = tool_input.get("file_path") or ""
    if not file_path or not os.path.isfile(file_path):
        sys.exit(0)

    normalized = file_path.replace("\\", "/")
    if any(seg in normalized for seg in EXEMPT_SEGMENTS) or normalized.endswith(".csproj"):
        sys.exit(0)

    # Allow if offset or limit is explicitly provided
    if tool_input.get("offset") is not None or tool_input.get("limit") is not None:
        sys.exit(0)

    try:
        with open(file_path, encoding="utf-8", errors="ignore") as f:
            line_count = sum(1 for _ in f)
    except OSError:
        sys.exit(0)

    if line_count > MAX_UNBOUNDED_LINES:
        print(
            json.dumps(
                {
                    "hookSpecificOutput": {
                        "hookEventName": "PreToolUse",
                        "permissionDecision": "deny",
                        "permissionDecisionReason": (
                            f"File has {line_count} lines (limit: {MAX_UNBOUNDED_LINES} for full reads). "
                            "Use Grep to locate the target section first, then call Read with "
                            "explicit `offset` and `limit` (<= 100 lines per slice)."
                        ),
                    }
                }
            )
        )


if __name__ == "__main__":
    main()