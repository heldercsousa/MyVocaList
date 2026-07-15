"""Deterministic tests for the fail-open Stop wrapper `orphan_check.py` (AC-6/12, INV-1).

These exercise the wrapper's I/O glue against fixture dirs and temp files — the pure
classification logic is covered by `test_backlog_lib.py` (Phase 3, frozen). All paths
are parameterized so nothing touches the real device-memory dir or real git state.
"""
import io
import os
import sys
import tempfile
import unittest
from contextlib import redirect_stdout

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import orphan_check  # noqa: E402


def _write(path, text):
    with open(path, "w", encoding="utf-8") as fh:
        fh.write(text)


class TestResolveDeviceDir(unittest.TestCase):
    """resolve_device_dir(device_memory_dir) — fixture injection + fail-open (AC-12)."""

    # [AC] AC-12: an injected fixture path is returned verbatim (parameterized, not mangled)
    def test_injected_path_returned_verbatim(self):
        fixture = os.path.join(tempfile.gettempdir(), "fixture_memory_dir")
        self.assertEqual(orphan_check.resolve_device_dir(fixture), fixture)

    # [AC] AC-6/INV-1: resolution never raises; absent param falls back to git derivation
    def test_no_param_does_not_raise(self):
        try:
            result = orphan_check.resolve_device_dir()
        except Exception as exc:  # pragma: no cover - guards against regressions
            self.fail("resolve_device_dir raised: {0!r}".format(exc))
        # Result is either a derived path string or None on git failure — both valid.
        self.assertTrue(result is None or isinstance(result, str))


class TestEnumerateChangedMemoryFiles(unittest.TestCase):
    """enumerate_changed_memory_files — fixture enumeration + fail-open (AC-12/AC-6)."""

    def setUp(self):
        self.tmp = tempfile.mkdtemp()
        # A fixture device-memory dir whose basename's PARENT mangle is reused as the
        # substring matched in the log: <tmp>/projects/<mangled>/memory
        self.mangled = "C--Users-test-repo"
        self.mem_dir = os.path.join(self.tmp, "projects", self.mangled, "memory")
        os.makedirs(self.mem_dir)
        self.log_path = os.path.join(self.tmp, "changed-files.txt")

    def _marker(self, offset, head=""):
        _write(orphan_check._MARKER_PATH, "{0}\n{1}\n".format(offset, head))
        self.addCleanup(self._rm_marker)

    def _rm_marker(self):
        try:
            os.remove(orphan_check._MARKER_PATH)
        except OSError:
            pass

    # [AC] AC-12: only memory-dir lines AFTER the session offset are enumerated
    def test_enumerates_only_session_memory_lines(self):
        lines = [
            # pre-session noise (before offset) — must be ignored
            "../../../.claude/projects/{0}/memory/MEMORY.md".format(self.mangled),
            "MyVocaList/SomeFile.cs",
            # session boundary is offset=2 -> lines below are this session
            "MyVocaList/Other.cs",
            "../../../.claude/projects/{0}/memory/project_new.md".format(self.mangled),
            "../../../.claude/projects/{0}/memory/MEMORY.md".format(self.mangled),
        ]
        _write(self.log_path, "\n".join(lines) + "\n")
        self._marker(offset=2)

        result = orphan_check.enumerate_changed_memory_files(
            signal_source=self.log_path, device_memory_dir=self.mem_dir
        )
        names = [name for name, _ in result]
        self.assertEqual(names, ["project_new.md", "MEMORY.md"])

    # [AC] AC-12: with no marker, fall back to reading all memory lines (fail-open)
    def test_missing_marker_reads_all_memory_lines(self):
        self._rm_marker()  # ensure absent
        lines = [
            "../../../.claude/projects/{0}/memory/a.md".format(self.mangled),
            "MyVocaList/X.cs",
            "../../../.claude/projects/{0}/memory/b.md".format(self.mangled),
        ]
        _write(self.log_path, "\n".join(lines) + "\n")

        result = orphan_check.enumerate_changed_memory_files(
            signal_source=self.log_path, device_memory_dir=self.mem_dir
        )
        names = sorted(name for name, _ in result)
        self.assertEqual(names, ["a.md", "b.md"])

    # [AC] AC-6: an unreadable / missing signal log -> [] (no raise)
    def test_missing_log_returns_empty(self):
        result = orphan_check.enumerate_changed_memory_files(
            signal_source=os.path.join(self.tmp, "does-not-exist.txt"),
            device_memory_dir=self.mem_dir,
        )
        self.assertEqual(result, [])

    # [AC] AC-6: unresolved device dir -> [] (no raise)
    def test_unresolved_device_dir_returns_empty(self):
        _write(self.log_path, "anything\n")
        result = orphan_check.enumerate_changed_memory_files(
            signal_source=self.log_path, device_memory_dir=None
        )
        # device_memory_dir=None triggers git derivation; in the test env this may or
        # may not resolve, but it must never raise and must return a list.
        self.assertIsInstance(result, list)


class TestMainFailOpen(unittest.TestCase):
    """main() ALWAYS returns 0 and never raises (AC-6 / INV-1)."""

    # [AC] AC-6/INV-1: missing signal source -> main() returns 0, no raise
    def test_main_missing_signal_returns_zero(self):
        with redirect_stdout(io.StringIO()):
            rc = orphan_check.main(
                device_memory_dir=None,
                signal_source="/nonexistent/changed-files.txt",
            )
        self.assertEqual(rc, 0)

    # [AC] AC-6/INV-1: a totally bogus device dir still yields exit 0
    def test_main_bogus_device_dir_returns_zero(self):
        with redirect_stdout(io.StringIO()):
            rc = orphan_check.main(
                device_memory_dir="/no/such/memory/dir",
                signal_source="/no/such/log.txt",
            )
        self.assertEqual(rc, 0)


class TestMainReminderBehavior(unittest.TestCase):
    """End-to-end print behavior through main() with a stubbed backlog signal."""

    def setUp(self):
        self.tmp = tempfile.mkdtemp()
        self.mangled = "C--Users-test-repo"
        self.mem_dir = os.path.join(self.tmp, "projects", self.mangled, "memory")
        os.makedirs(self.mem_dir)
        self.log_path = os.path.join(self.tmp, "changed-files.txt")
        # No marker -> read all lines (deterministic for these tests).
        try:
            os.remove(orphan_check._MARKER_PATH)
        except OSError:
            pass
        # Stub the git-dependent backlog check so tests are deterministic.
        self._orig_backlog = orphan_check.backlog_changed_this_session

    def tearDown(self):
        orphan_check.backlog_changed_this_session = self._orig_backlog

    def _run(self):
        buf = io.StringIO()
        with redirect_stdout(buf):
            rc = orphan_check.main(
                device_memory_dir=self.mem_dir, signal_source=self.log_path
            )
        return rc, buf.getvalue()

    # [AC] AC-5: candidate memory write + BACKLOG unchanged -> reminder printed.
    # The wrapper feeds each matched log LINE to the (frozen) classifier, which
    # returns 'candidate' when a new-work verb appears as a whole word — so the
    # fixture log line carries the verb to drive the candidate verdict.
    def test_candidate_backlog_unchanged_prints_reminder(self):
        _write(
            self.log_path,
            "implement new export service "
            "projects/{0}/memory/project_x.md\n".format(self.mangled),
        )
        orphan_check.backlog_changed_this_session = lambda: False

        rc, out = self._run()
        self.assertEqual(rc, 0)
        self.assertIn("BACKLOG", out)

    # [AC] AC-5: candidate memory write but BACKLOG changed -> reminder suppressed
    def test_candidate_backlog_changed_suppresses_reminder(self):
        _write(
            self.log_path,
            "implement new export service "
            "projects/{0}/memory/project_x.md\n".format(self.mangled),
        )
        orphan_check.backlog_changed_this_session = lambda: True

        rc, out = self._run()
        self.assertEqual(rc, 0)
        self.assertEqual(out.strip(), "")

    # [AC] AC-7/AC-11: only exempt memory writes -> no reminder (no false positive)
    def test_all_exempt_prints_nothing(self):
        _write(
            self.log_path,
            "NEXT: continue smoke tests "
            "projects/{0}/memory/feedback_loop.md\n".format(self.mangled),
        )
        orphan_check.backlog_changed_this_session = lambda: False

        rc, out = self._run()
        self.assertEqual(rc, 0)
        self.assertEqual(out.strip(), "")

    # [AC] AC-7: no memory writes at all -> no reminder
    def test_no_memory_writes_prints_nothing(self):
        _write(self.log_path, "MyVocaList/Only.cs\n")
        orphan_check.backlog_changed_this_session = lambda: False

        rc, out = self._run()
        self.assertEqual(rc, 0)
        self.assertEqual(out.strip(), "")


if __name__ == "__main__":
    unittest.main()
