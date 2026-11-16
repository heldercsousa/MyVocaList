"""Tests for tasklog_rotate.py - only ready entries rotate; open ones stay."""
import datetime
import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import tasklog_rotate as tr  # noqa: E402

LOG = """# Task log: sample

Status line.

## 2026-10-30 — A1 Contracts (Done)

text a1

## 2026-11-02 — A2 Service (To Review)

text a2

## T3 — Close-out (2026-11-03) — Done

- [ ] an unticked box holds this entry

## 2026-11-04 — A4 Merge (Done; device check open)

text a4

## 2026-11-05 — A5 Work (Done)

### Checkpoint
live block

## Device check (Helder)

1. step

## A6 — No date (Done)

text a6
"""


class RotateTests(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.mkdtemp()
        self.log = os.path.join(self.dir, "task-log.md")
        with open(self.log, "w", encoding="utf-8", newline="") as f:
            f.write(LOG)
        self.cwd = os.getcwd()
        os.chdir(self.dir)

    def tearDown(self):
        os.chdir(self.cwd)

    def read(self, name):
        with open(os.path.join(self.dir, name), encoding="utf-8") as f:
            return f.read()

    def test_open_item_rotates_only_done_entries(self):
        tr.rotate(self.log, today=datetime.date(2026, 11, 15))
        log = self.read("task-log.md")
        for kept in ("A2 Service", "T3 — Close-out", "A4 Merge", "A5 Work", "Device check", "Status line."):
            self.assertIn(kept, log)
        self.assertNotIn("A1 Contracts", log)
        self.assertNotIn("A6 — No date", log)
        self.assertIn("A1 Contracts", self.read("task-log-archive-2026-10.md"))
        self.assertIn("text a6", self.read("task-log-archive-2026-11.md"))

    def test_closed_item_rotates_every_entry_and_keeps_header(self):
        with open(os.path.join(self.dir, "README.md"), "w", encoding="utf-8") as f:
            f.write("---\nid: x\nstatus: ✅ Done\n---\n# x\n")
        tr.rotate(self.log, today=datetime.date(2026, 11, 15))
        log = self.read("task-log.md")
        self.assertIn("Status line.", log)
        self.assertNotIn("## ", log)

    def test_nothing_ready_changes_nothing(self):
        text = "# Log\n\n## 2026-11-02 — A2 (To Review)\n\nx\n"
        with open(self.log, "w", encoding="utf-8", newline="") as f:
            f.write(text)
        tr.rotate(self.log)
        self.assertEqual(text, self.read("task-log.md"))
        self.assertEqual(0, tr.ready_count(text, self.dir))

    def test_dry_run_writes_nothing(self):
        tr.rotate(self.log, dry_run=True)
        self.assertEqual(LOG, self.read("task-log.md"))
        self.assertFalse(os.path.exists(os.path.join(self.dir, "task-log-archive-2026-10.md")))

    def test_second_rotation_appends_to_the_same_archive(self):
        tr.rotate(self.log, today=datetime.date(2026, 11, 15))
        with open(self.log, "a", encoding="utf-8", newline="") as f:
            f.write("\n## 2026-11-20 — A7 Late (Done)\n\ntext a7\n")
        tr.rotate(self.log, today=datetime.date(2026, 11, 21))
        archive = self.read("task-log-archive-2026-11.md")
        self.assertIn("text a6", archive)
        self.assertIn("text a7", archive)


if __name__ == "__main__":
    unittest.main()
