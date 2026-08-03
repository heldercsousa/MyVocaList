"""Guard: every documented `backlog_gen.py` invocation must actually parse.

Three real incidents motivated this file -- each was documentation describing
a command that had never been run: an invented lease-claim API, a
`--kind activity` that is not a valid `--kind` choice (only `bug`/`change`
are), and a `--status "..."` value that parsed but silently matched nothing.
The generator was correct each time; the prose about it was wrong. This test
makes the mechanical part of that check (verb exists, every `--flag` is real,
every documented value that declares `choices` is a real choice, required
flags/positionals are present) run automatically instead of waiting for a
human to type the command and notice.

Self-maintaining by construction: it walks every `*.md` under `.claude/` plus
`CLAUDE.md`, and validates against `backlog_gen.build_parser()` itself --
never a hand-copied list of flags. A new documented command is covered with
no edit to this file; a new flag added to `backlog_gen.py` is picked up
automatically because the parser is introspected live.
"""
import argparse
import glob
import os
import re
import shlex
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import backlog_gen  # noqa: E402

TESTS_DIR = os.path.dirname(os.path.abspath(__file__))
BACKLOG_DIR = os.path.dirname(TESTS_DIR)
SCRIPTS_DIR = os.path.dirname(BACKLOG_DIR)
CLAUDE_DIR = os.path.dirname(SCRIPTS_DIR)
ROOT = os.path.dirname(CLAUDE_DIR)

PROG = "backlog_gen.py"

FENCE_RE = re.compile(r"```[a-zA-Z]*\n(.*?)```", re.S)
INLINE_RE = re.compile(r"`([^`]*?)`", re.S)


def _subparser_map():
    """{verb: ArgumentParser} straight from the real parser -- never hand-copied."""
    parser = backlog_gen.build_parser()
    action = next(a for a in parser._actions
                 if isinstance(a, argparse._SubParsersAction))
    return dict(action.choices)


def _is_placeholder(token):
    """Decide whether a documented value is real prose or a stand-in.

    Deliberate rule (see module docstring / task brief): a token that is
    ENTIRELY placeholder syntax -- wrapped in `<...>` (incl. `<a|b|c>`
    alternatives), the literal ellipsis character `...`/`…`, or empty
    after quote-stripping -- is never checked against `choices`, because it
    was never meant to be a concrete value. A bare word like `activity` is a
    concrete literal and MUST be checked -- that is exactly the shape of the
    historical `--kind activity` defect this guard exists to catch.
    """
    text = token.strip()
    if text and text[0] in "\"'" and text[-1:] == text[0] and len(text) >= 2:
        text = text[1:-1]
    text = text.strip()
    if text == "":
        return True
    if text.startswith("<") and text.endswith(">"):
        return True
    if "…" in text or text == "...":
        return True
    return False


def _logical_lines(block, start_line):
    """Yield (line_no, text) for `block`, joining `\\`-continued lines.

    A continuation line contributes no extra invocations of its own -- it is
    physically part of the command that started it (design brief: the
    multi-line `register` example in workflow-rule-1.md must be picked up as
    ONE invocation, not one dangling fragment per physical line).
    """
    lines = block.split("\n")
    i = 0
    while i < len(lines):
        line_no = start_line + i
        buf = lines[i].rstrip()
        while buf.endswith("\\"):
            buf = buf[:-1].rstrip()
            i += 1
            if i >= len(lines):
                break
            buf = buf + " " + lines[i].strip()
        yield line_no, buf
        i += 1


def _tail_from_prog(text):
    """Slice `text` to start at the `backlog_gen.py` token, or None if absent."""
    idx = text.find(PROG)
    if idx == -1:
        return None
    return text[idx:].strip()


def extract_invocations(text):
    """Return [(line_no, source, raw_tail)] for every `backlog_gen.py` mention.

    `source` is `"fenced"` for a ``` code block (treated as a complete,
    runnable, copy-pasteable example -> required-arg/positional-count checks
    apply) or `"inline"` for a single/double-backtick span embedded in prose
    (frequently an abbreviated reference to just the verb, e.g. "register it
    with `backlog_gen.py register`" -- flag/choices validity is still
    checked, but missing required args is NOT an error there; see
    `_validate`).
    """
    results = []

    for m in FENCE_RE.finditer(text):
        block = m.group(1)
        block_start_line = text.count("\n", 0, m.start(1)) + 1
        for line_no, logical in _logical_lines(block, block_start_line):
            if PROG in logical:
                tail = _tail_from_prog(logical)
                if tail:
                    results.append((line_no, "fenced", tail))

    # Blank out fenced blocks (preserving line count) before the inline scan,
    # so a ``` marker line is never re-matched as a single-backtick span.
    text_wo_fences = FENCE_RE.sub(lambda m: "\n" * m.group(0).count("\n"), text)
    for m in INLINE_RE.finditer(text_wo_fences):
        content = m.group(1)
        if PROG not in content:
            continue
        line_no = text_wo_fences.count("\n", 0, m.start()) + 1
        joined = re.sub(r"\s+", " ", content).strip()
        tail = _tail_from_prog(joined)
        if tail:
            results.append((line_no, "inline", tail))

    return results


def _tokenize(raw_tail):
    """Tokens after the verb: strip optional-group brackets, then shlex-split.

    `[...]` marks an optional group in these docs (e.g. `[--gate "..."]`) --
    it is not shell syntax, so the literal `[`/`]` characters are dropped
    before splitting rather than treated as part of a token.
    """
    stripped = raw_tail.replace("[", " ").replace("]", " ")
    try:
        tokens = shlex.split(stripped, posix=True)
    except ValueError:
        return None
    # tokens[0] should be exactly "backlog_gen.py" (that's where the tail was
    # sliced from); a shlex quirk (e.g. an unbalanced quote survives bracket
    # stripping) can shift this -- treat as unparseable rather than guess.
    if not tokens or tokens[0] != PROG:
        return None
    return tokens[1:]


def _validate(verb_tokens, source, parser_map):
    """Return a list of human-readable error strings for one invocation."""
    errors = []
    if not verb_tokens:
        return errors  # bare "backlog_gen.py" with nothing after it -- ignore
    verb, rest = verb_tokens[0], verb_tokens[1:]
    if verb not in parser_map:
        return ["unknown verb '{0}' -- valid verbs: {1}".format(
            verb, ", ".join(sorted(parser_map)))]
    if not rest:
        return errors  # bare verb mention, e.g. "`backlog_gen.py register`"

    sp = parser_map[verb]
    flag_actions = {}
    for a in sp._actions:
        for opt in a.option_strings:
            flag_actions[opt] = a
    positional_actions = [a for a in sp._actions
                          if not a.option_strings and a.dest != "help"]

    seen_dests = set()
    positionals_given = []
    i = 0
    while i < len(rest):
        tok = rest[i]
        if tok.startswith("--"):
            if "=" in tok:
                flag, val = tok.split("=", 1)
                has_val = True
            else:
                flag, val = tok, None
                has_val = False
            action = flag_actions.get(flag)
            if action is None:
                valid = sorted(o for o in flag_actions if o != "--help" and o != "-h")
                errors.append(
                    "{0}: not a valid flag for verb '{1}' (valid: {2})".format(
                        flag, verb, ", ".join(valid)))
                i += 1
                continue
            seen_dests.add(action.dest)
            takes_value = not isinstance(action, argparse._StoreTrueAction)
            if takes_value and not has_val:
                if i + 1 < len(rest):
                    val = rest[i + 1]
                    i += 1
                else:
                    errors.append("{0}: expects a value but none follows".format(flag))
            if takes_value and action.choices and val is not None:
                if not _is_placeholder(val):
                    if val not in action.choices:
                        errors.append(
                            "{0} {1}: not in choices {2}".format(
                                flag, val, ", ".join(action.choices)))
            i += 1
        else:
            positionals_given.append(tok)
            i += 1

    required_dests = {a.dest for a in sp._actions if a.required and not a.option_strings}
    required_flag_dests = {a.dest: a.option_strings[0] for a in sp._actions
                           if a.required and a.option_strings}
    if source == "fenced":
        missing_flags = [flag for dest, flag in required_flag_dests.items()
                         if dest not in seen_dests]
        if missing_flags:
            errors.append("missing required flag(s): {0}".format(", ".join(sorted(missing_flags))))

    if len(positionals_given) > len(positional_actions):
        errors.append("too many positional arguments: got {0}, expected {1}".format(
            len(positionals_given), len(positional_actions)))
    elif source == "fenced" and len(positionals_given) < len(positional_actions):
        errors.append("missing positional argument(s): expected {0}, got {1}".format(
            len(positional_actions), len(positionals_given)))

    return errors


def scan_and_validate(md_text, parser_map):
    """Return [(line_no, source, raw_tail, [errors])] for every invocation found."""
    out = []
    for line_no, source, raw_tail in extract_invocations(md_text):
        tokens = _tokenize(raw_tail)
        if tokens is None:
            out.append((line_no, source, raw_tail,
                       ["could not tokenize invocation (unbalanced quotes?)"]))
            continue
        errors = _validate(tokens, source, parser_map)
        out.append((line_no, source, raw_tail, errors))
    return out


def _doc_files(root):
    files = sorted(glob.glob(os.path.join(root, ".claude", "**", "*.md"), recursive=True))
    claude_md = os.path.join(root, "CLAUDE.md")
    if os.path.exists(claude_md):
        files.append(claude_md)
    return files


class CommandDocsGuardTests(unittest.TestCase):

    def setUp(self):
        self.parser_map = _subparser_map()

    def test_every_documented_invocation_parses(self):
        failures = []
        total = 0
        files_with_invocations = 0
        for path in _doc_files(ROOT):
            with open(path, encoding="utf-8-sig") as fh:
                text = fh.read()
            results = scan_and_validate(text, self.parser_map)
            if results:
                files_with_invocations += 1
            for line_no, source, raw_tail, errors in results:
                total += 1
                for err in errors:
                    rel = os.path.relpath(path, ROOT).replace("\\", "/")
                    failures.append(
                        "{0}:{1}: [{2}] `{3}`\n    -> {4}".format(
                            rel, line_no, source, raw_tail, err))

        # Sanity floor: the extraction is meant to find every documented
        # command, not just the ones a hand-written list would remember. If
        # this drops far below the ~30 known real mentions, the scanner
        # itself regressed silently.
        self.assertGreaterEqual(
            total, 20,
            "scanner found only {0} invocation(s) across {1} file(s) -- "
            "expected >= 20; extraction likely regressed".format(
                total, files_with_invocations))

        if failures:
            self.fail(
                "{0} documented backlog_gen.py invocation(s) do not parse "
                "against the real argparse parser:\n\n{1}".format(
                    len(failures), "\n".join(failures)))

    def test_guard_catches_invalid_kind_choice(self):
        """Reintroduce the historical `--kind activity` defect in a scratch
        fixture (never in real docs) and confirm the guard reports it with an
        actionable message naming the offending value."""
        fixture = (
            "# Scratch fixture -- not a real doc\n\n"
            "```bash\n"
            "python .claude/scripts/backlog/backlog_gen.py register \\\n"
            "    --parent PARENT-1 --kind activity \\\n"
            "    --title \"T\" --goal \"G\"\n"
            "```\n"
        )
        results = scan_and_validate(fixture, self.parser_map)
        self.assertEqual(1, len(results))
        _line_no, source, _raw, errors = results[0]
        self.assertEqual("fenced", source)
        self.assertTrue(
            any("--kind activity" in e and "not in choices" in e for e in errors),
            "expected a '--kind activity: not in choices ...' error, got: {0}".format(errors))

    def test_guard_passes_the_valid_form_of_the_same_fixture(self):
        fixture = (
            "```bash\n"
            "python .claude/scripts/backlog/backlog_gen.py register \\\n"
            "    --parent PARENT-1 --kind bug \\\n"
            "    --title \"T\" --goal \"G\"\n"
            "```\n"
        )
        results = scan_and_validate(fixture, self.parser_map)
        self.assertEqual(1, len(results))
        _line_no, _source, _raw, errors = results[0]
        self.assertEqual([], errors)

    def test_multiline_register_example_is_one_invocation(self):
        """The continuation-line join must not split one command into pieces."""
        fixture = (
            "```bash\n"
            "python .claude/scripts/backlog/backlog_gen.py register \\\n"
            "    --parent <parent-id> --kind <bug|change> [--severity <Critical|Major|Minor>] \\\n"
            "    --title \"...\" --goal \"...\" [--gate \"...\"] [--section BusinessFeatures|DevCycleCraft]\n"
            "```\n"
        )
        results = scan_and_validate(fixture, self.parser_map)
        self.assertEqual(1, len(results), "expected one joined invocation, got {0}".format(results))
        _line_no, _source, _raw, errors = results[0]
        self.assertEqual([], errors)

    def test_bare_verb_mention_is_not_flagged_for_missing_required_args(self):
        fixture = "Register it with `backlog_gen.py register` so it gets a row.\n"
        results = scan_and_validate(fixture, self.parser_map)
        self.assertEqual(1, len(results))
        _line_no, source, _raw, errors = results[0]
        self.assertEqual("inline", source)
        self.assertEqual([], errors)

    def test_unknown_flag_is_reported(self):
        fixture = "`backlog_gen.py query --statuz Ready`\n"
        results = scan_and_validate(fixture, self.parser_map)
        self.assertEqual(1, len(results))
        _line_no, _source, _raw, errors = results[0]
        self.assertTrue(
            any("--statuz" in e and "not a valid flag" in e for e in errors), errors)


if __name__ == "__main__":
    unittest.main()
