# .claude/scripts/backlog/tests/test_horizon.py
"""`horizon: post-mvp` -- REQ-TUR-03 of the token-usage reduction item (2026-11-15).

Post-MVP items render into BACKLOG-POST-MVP.md, not BACKLOG.md; `query` leaves them
out unless asked; archiving is unchanged.
"""
import glob
import os
import shutil
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import backlog_gen  # noqa: E402
import model  # noqa: E402
from test_backlog_gen import (BACKLOG_SKELETON, PENDING, SLN_FIXTURE,  # noqa: E402
                              captured_stderr, readme, write)

DONE = "✅ Done"


class HorizonTests(unittest.TestCase):
    def setUp(self):
        self.root = tempfile.mkdtemp()
        self.mgmt = os.path.join(self.root, "Docs", "Management")
        self.backlog = os.path.join(self.mgmt, "BACKLOG.md")
        self.post = os.path.join(self.mgmt, backlog_gen.POST_MVP_FILE)
        write(self.backlog, BACKLOG_SKELETON)
        write(os.path.join(self.root, "MyVocaList.sln"), SLN_FIXTURE)
        self._item("now", id="NOW", title="Ship now")
        self._item("later", id="LATER", title="Ship later", horizon="post-mvp")

    def tearDown(self):
        shutil.rmtree(self.root, ignore_errors=True)

    def _item(self, folder, **keys):
        write(os.path.join(self.mgmt, "BusinessFeatures", *folder.split("/"), "README.md"),
              readme(**keys))

    def _read(self, path):
        with open(path, encoding="utf-8") as fh:
            return fh.read()

    def _regen(self, check=False):
        with captured_stderr() as err:
            rc = backlog_gen.cmd_regen(self.root, check=check)
        return rc, err.getvalue()

    def test_post_mvp_item_renders_only_in_the_post_mvp_file(self):
        """AC-03.1 / AC-03.2"""
        self.assertEqual(self._regen()[0], 0)
        main, post = self._read(self.backlog), self._read(self.post)
        self.assertIn("Ship now", main)
        self.assertNotIn("Ship later", main)
        self.assertIn("Ship later", post)
        self.assertNotIn("Ship now", post)

    def test_no_post_mvp_file_is_created_when_no_item_has_the_key(self):
        """AC-03.2: a tree without the key behaves exactly as before."""
        self._item("later", id="LATER", title="Ship later")
        self.assertEqual(self._regen()[0], 0)
        self.assertFalse(os.path.exists(self.post))

    def test_terminal_post_mvp_item_is_archived_like_any_other(self):
        """AC-03.3"""
        self._item("later", id="LATER", title="Ship later", horizon="post-mvp",
                   status=DONE, closed="2026-11")
        self.assertEqual(self._regen()[0], 0)
        archives = glob.glob(os.path.join(self.mgmt, "backlog-archive", "*2026-11.md"))
        self.assertEqual(len(archives), 1)
        self.assertIn("Ship later", self._read(archives[0]))
        self.assertNotIn("Ship later", self._read(self.backlog))

    def test_query_leaves_post_mvp_out_unless_asked(self):
        """AC-03.4"""
        titles = lambda h: " ".join(backlog_gen.query_lines(self.root, [""], h))  # noqa: E731
        self.assertIn("Ship now", titles("mvp"))
        self.assertNotIn("Ship later", titles("mvp"))
        self.assertIn("Ship later", titles("post-mvp"))
        self.assertNotIn("Ship now", titles("post-mvp"))
        self.assertIn("Ship now", titles("all"))
        self.assertIn("Ship later", titles("all"))
        self.assertNotIn("Ship later", " ".join(backlog_gen.query_lines(self.root, [""])))

    def test_check_fails_when_the_post_mvp_file_is_stale(self):
        """AC-03.5"""
        self.assertEqual(self._regen()[0], 0)
        self.assertEqual(self._regen(check=True)[0], 0)
        self._item("later2", id="LATER2", title="Another later", horizon="post-mvp")
        self.assertNotEqual(self._regen(check=True)[0], 0)

    def test_unknown_horizon_aborts_regeneration_naming_the_item(self):
        """AC-03.6"""
        self._item("odd", id="ODD", title="Odd one", horizon="someday")
        rc, err = self._regen()
        self.assertNotEqual(rc, 0)
        self.assertIn("invalid horizon 'someday'", err)
        self.assertIn("odd", err)

    def test_register_writes_the_key(self):
        """AC-03.7"""
        with captured_stderr():
            rc = backlog_gen.cmd_register(self.root, None, "NOW", "change", None,
                                          "Later idea", "Do it later.", None,
                                          today="2026-11-15", horizon="post-mvp")
        self.assertEqual(rc, 0)
        created = glob.glob(os.path.join(self.mgmt, "BusinessFeatures", "now", "changes",
                                         "*", "README.md"))
        self.assertEqual(len(created), 1)
        self.assertIn("horizon: post-mvp", self._read(created[0]))
        self.assertIn("Later idea", self._read(self.post))
        self.assertNotIn("Later idea", self._read(self.backlog))

    def test_child_inherits_the_parent_horizon_unless_it_sets_its_own(self):
        """AC-03.9"""
        parent = model.Item({"id": "P", "horizon": "post-mvp"}, "BusinessFeatures/p/")
        child = model.Item({"id": "C", "parent": "P"}, "BusinessFeatures/p/changes/c/")
        plain = model.Item({"id": "Q"}, "BusinessFeatures/q/")
        own = model.Item({"id": "O", "parent": "Q", "horizon": "post-mvp"},
                         "BusinessFeatures/q/changes/o/")
        pool = [parent, child, plain, own]
        self.assertTrue(model.is_post_mvp(child, pool))
        self.assertTrue(model.is_post_mvp(own, pool))
        self.assertFalse(model.is_post_mvp(plain, pool))

    def test_changing_the_key_moves_the_row_and_it_is_in_exactly_one_file(self):
        """AC-03.10"""
        self._regen()
        self._item("now", id="NOW", title="Ship now", horizon="post-mvp")
        self._item("later", id="LATER", title="Ship later")
        self.assertEqual(self._regen()[0], 0)
        main, post = self._read(self.backlog), self._read(self.post)
        self.assertEqual((main.count("Ship now"), post.count("Ship now")), (0, 1))
        self.assertEqual((main.count("Ship later"), post.count("Ship later")), (1, 0))

    def test_removing_the_last_post_mvp_item_empties_the_file_tables(self):
        self._regen()
        self._item("later", id="LATER", title="Ship later")
        self.assertEqual(self._regen()[0], 0)
        self.assertNotIn("Ship later", self._read(self.post))


if __name__ == "__main__":
    unittest.main()
