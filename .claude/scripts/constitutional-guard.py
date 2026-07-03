#!/usr/bin/env python
"""PreToolUse blocking guard for unamendable constitutional constraints.

Blocks a Write/Edit that would INTRODUCE a native-dialog call into a
.cs / .xaml / .xaml.cs file. Native dialogs (DisplayAlert / DisplayActionSheet
/ DisplayPromptAsync) are forbidden by CLAUDE.md
(Constitutional Constraints -> Native dialogs, [Unamendable]). Use dx:BottomSheet.

Design notes:
- Only the NEW text is scanned (Write.content or Edit.new_string), so the guard
  fires only when a call is being added -- not when an unrelated edit touches a
  file that already (legitimately) mentions the term in a comment.
- Match requires the call form `Name(` (optional whitespace before the paren) so
  that prose/comments referencing the API by name are not false-positived.
- Fail-open: any internal error exits 0 so the guard can never break the workflow.
- Blocks via exit code 2 (PreToolUse: stderr is surfaced to the agent, tool call
  is denied).
"""
import json
import re
import sys

# Call-form patterns: the API name followed by an opening paren.
_FORBIDDEN = re.compile(r"\bDisplay(?:Alert|ActionSheet|PromptAsync)\s*\(")
_CODE_SUFFIXES = (".cs", ".xaml", ".xaml.cs")


def main() -> int:
    try:
        data = json.load(sys.stdin)
    except Exception:
        return 0  # fail-open: unreadable payload must not block tools

    tool_input = data.get("tool_input", {}) or {}
    file_path = (tool_input.get("file_path") or "").replace("\\", "/")
    if not file_path.endswith(_CODE_SUFFIXES):
        return 0

    # New text only: Write -> content; Edit -> new_string; MultiEdit -> each edit.
    fragments = []
    if isinstance(tool_input.get("content"), str):
        fragments.append(tool_input["content"])
    if isinstance(tool_input.get("new_string"), str):
        fragments.append(tool_input["new_string"])
    for edit in tool_input.get("edits", []) or []:
        if isinstance(edit, dict) and isinstance(edit.get("new_string"), str):
            fragments.append(edit["new_string"])

    for text in fragments:
        m = _FORBIDDEN.search(text)
        if m:
            call = m.group(0).rstrip("(").strip()
            sys.stderr.write(
                f"BLOCKED — constitutional constraint (CLAUDE.md, [Unamendable]).\n"
                f"This edit introduces `{call}(` into {file_path}.\n"
                f"Native dialogs (DisplayAlert / DisplayActionSheet / DisplayPromptAsync) "
                f"are forbidden — they bypass the app theme, violate MD3 interaction "
                f"patterns, and are not back-gesture dismissible on Android.\n"
                f"Use `dx:BottomSheet` instead — see the `myvocalist-coding` skill "
                f"(dialogs-validation.md). This constraint requires architecture review "
                f"to change; it cannot be relaxed at session level.\n"
            )
            return 2  # deny the tool call

    return 0


if __name__ == "__main__":
    sys.exit(main())
