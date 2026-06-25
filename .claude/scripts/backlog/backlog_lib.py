"""Pure, side-effect-free BACKLOG-orphan classification logic.

Unit-testable; no hook I/O, no file writes, stdlib only. Mirrors the pure-function
style of `lease_lib.py`.

Two public functions:
- `classify_memory_change` decides whether a single changed memory line represents
  genuinely new work that should have been registered in BACKLOG.md ('candidate')
  or is an exempt continuation / reference / auto-capture ('exempt').
- `should_remind` decides whether the Stop advisory should fire, given the set of
  classified changes and whether BACKLOG.md changed this session.
"""

# New-work verbs: a line proposing genuinely new, un-tracked work uses one of these.
# Presence of a verb is necessary but NOT sufficient — a verb targeting an
# already-tracked item is a continuation, not new work (AC-13 precedence).
_NEW_WORK_VERBS = (
    "implement",
    "add",
    "build",
    "create",
    "fix",
    "investigate",
)

# Markers that a line is a pure continuation of an already-tracked work item.
# When present, the new-work verb does not win — the line is exempt (AC-13).
_TRACKED_MARKERS = (
    "already tracked",
    "already-tracked",
)


def classify_memory_change(filename, line_or_diff):
    """Classify a single changed memory line.

    Returns 'exempt' or 'candidate' — never raises, never a third state.

    A new-work line inside MEMORY.md is a 'candidate' even though MEMORY.md is
    otherwise auto-captured (INV-3): classification is line/content-level, not
    file-level. Applies the documented precedence rule when an exempt marker and
    a new-work verb co-occur on one line (AC-13): the new-work verb only wins when
    it targets a noun that is not already tracked.
    """
    # AC-6: empty / None / whitespace / garbage -> exempt, never raises.
    if line_or_diff is None:
        return "exempt"
    line = str(line_or_diff).strip()
    if not line:
        return "exempt"

    name = "" if filename is None else str(filename)
    lowered = line.lower()

    # AC-4 category 1: feedback_* learnings are exempt regardless of content.
    if name.startswith("feedback_"):
        return "exempt"

    # AC-13: a pure continuation of an already-tracked item is exempt even when a
    # new-work verb is present.
    if any(marker in lowered for marker in _TRACKED_MARKERS):
        return "exempt"

    # New-work signal: a new-work verb targeting an un-tracked noun -> candidate.
    # The verb must appear as a standalone word, not as a substring of another word.
    words = _word_set(lowered)
    if any(verb in words for verb in _NEW_WORK_VERBS):
        return "candidate"

    # AC-4: reference caches (category 3), harness-automatic captures (category 4),
    # and project_* pure-continuation pointers (category 2) carry no new-work verb
    # -> exempt.
    return "exempt"


def should_remind(classified_changes, backlog_changed_this_session):
    """Return (should_remind, message).

    (False, _) whenever backlog_changed_this_session is True, regardless of
    candidates (AC-5 suppression). (True, reminder) iff at least one change is
    'candidate' AND BACKLOG was not changed this session. (False, _) otherwise —
    empty list or all-exempt (AC-7, no false positive).
    """
    if backlog_changed_this_session:
        return (False, "")
    if any(change == "candidate" for change in (classified_changes or [])):
        return (
            True,
            "BACKLOG-first: a memory note looks like new work that is not "
            "registered in Docs/Management/BACKLOG.md. Add a BACKLOG entry "
            "before proceeding.",
        )
    return (False, "")


def _word_set(lowered_line):
    """Split a lowercased line into a set of alphanumeric words for whole-word
    verb matching. Pure helper — no I/O."""
    word = []
    words = set()
    for ch in lowered_line:
        if ch.isalnum():
            word.append(ch)
        elif word:
            words.add("".join(word))
            word = []
    if word:
        words.add("".join(word))
    return words


if __name__ == "__main__":
    # Importable smoke check: no output expected on clean import.
    pass
