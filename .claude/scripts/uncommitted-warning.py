"""Uncommitted-changes warnings (Rule 3: commit after every task). Never blocks.

  --after-agent  PostToolUse (Agent): a subagent returned and the tree is dirty. Returned as
                 additionalContext, so the model sees it and can commit.
  --stop         Stop hook: returned as systemMessage, shown to the user only. A Stop message
                 the model could see needs `decision: block`, which forces another turn.
"""
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import hook_out  # noqa: E402


def changed_paths():
    proj = os.environ.get("CLAUDE_PROJECT_DIR", ".")
    r = subprocess.run(["git", "status", "--porcelain"], cwd=proj,
                       capture_output=True, text=True, timeout=15)
    return [ln[3:] for ln in r.stdout.splitlines() if ln.strip()]


def stop_message(paths):
    if not paths:
        return ""
    msg = ["UNCOMMITTED CHANGES - build + test, then /sln-commit."]
    if any("task-log" in p for p in paths) and not any(p.endswith("README.md") for p in paths):
        msg.append("task-log changed but no item README.md - if a feature phase completed, "
                   "run backlog_gen.py status.")
    return " ".join(msg)


def main() -> int:
    try:
        paths = changed_paths()
        if not paths:
            return 0
        if "--after-agent" in sys.argv:
            hook_out.additional_context(
                "PostToolUse",
                "Subagent returned with uncommitted changes in the main checkout - "
                "run /sln-commit now.")
        elif "--stop" in sys.argv:
            hook_out.system_message(stop_message(paths))
    except Exception:
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main())
