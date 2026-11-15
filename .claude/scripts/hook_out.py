"""JSON output forms for Claude Code hooks.

Plain `print` from a tool or Stop hook that exits 0 is not delivered to the model. Use:
  additional_context(event, text) -> the model sees `text` (costs tokens each time it fires)
  system_message(text)            -> the user sees `text`; the model does not
  deny(reason)                    -> PreToolUse only: block the tool call, the model sees `reason`
"""
import json
import sys


def _emit(obj) -> None:
    sys.stdout.write(json.dumps(obj) + "\n")


def additional_context(event: str, text: str) -> None:
    _emit({"hookSpecificOutput": {"hookEventName": event, "additionalContext": text}})


def system_message(text: str) -> None:
    _emit({"systemMessage": text})


def deny(reason: str) -> None:
    _emit({"hookSpecificOutput": {
        "hookEventName": "PreToolUse",
        "permissionDecision": "deny",
        "permissionDecisionReason": reason,
    }})
