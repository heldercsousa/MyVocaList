import os
import sys
import unittest
from datetime import datetime, timezone, timedelta

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import lease_lib  # noqa: E402

NOW = datetime(2026, 6, 14, 12, 0, 0, tzinfo=timezone.utc)


def iso(dt):
    return dt.isoformat()


class TestClassify(unittest.TestCase):
    # [AC] AC-1.1: last_active within TTL -> fresh
    def test_within_ttl_is_fresh(self):
        claim = {"owner": "s1", "last_active": iso(NOW - timedelta(minutes=5)), "pid": None}
        self.assertEqual(
            lease_lib.classify(claim, now=NOW, pid_alive_fn=lambda p: False), "fresh")

    # [AC] AC-1.2: last_active stale BUT live same-host pid -> fresh
    def test_old_ttl_but_live_pid_is_fresh(self):
        claim = {"owner": "s1", "last_active": iso(NOW - timedelta(minutes=40)), "pid": 999}
        self.assertEqual(
            lease_lib.classify(claim, now=NOW, pid_alive_fn=lambda p: True), "fresh")

    # [AC] AC-2.1: stale + dead/no pid -> stale/reclaimable
    def test_old_ttl_and_dead_pid_is_stale(self):
        claim = {"owner": "s1", "last_active": iso(NOW - timedelta(minutes=40)), "pid": 999}
        self.assertEqual(
            lease_lib.classify(claim, now=NOW, pid_alive_fn=lambda p: False), "stale")

    # [AC] AC-2.1: stale + no pid recorded -> stale
    def test_stale_with_no_pid_is_stale(self):
        claim = {"owner": "s1", "last_active": iso(NOW - timedelta(minutes=40))}
        self.assertEqual(
            lease_lib.classify(claim, now=NOW, pid_alive_fn=lambda p: False), "stale")

    # [AC] AC-2.2: dead same-host pid past TTL -> reclaimable now (fast path: stale)
    def test_dead_pid_after_ttl_is_stale_for_fast_reclaim(self):
        claim = {"owner": "s1", "last_active": iso(NOW - timedelta(minutes=31)), "pid": 12345}
        self.assertEqual(
            lease_lib.classify(claim, now=NOW, pid_alive_fn=lambda p: False), "stale")

    # [AC] AC-2.5: corrupt / unparseable / half-written claim -> treated as absent/stale
    def test_corrupt_claim_is_stale(self):
        self.assertIsNone(lease_lib.parse_claim('{"owner": "s1"'))  # half-written
        self.assertIsNone(lease_lib.parse_claim(""))                 # empty
        self.assertIsNone(lease_lib.parse_claim("   "))              # whitespace
        self.assertIsNone(lease_lib.parse_claim("not json at all"))  # garbage
        self.assertIsNone(lease_lib.parse_claim(None))               # None
        self.assertIsNone(lease_lib.parse_claim("[1,2,3]"))          # not a dict
        self.assertIsNone(lease_lib.parse_claim('{"owner":"s1"}'))   # missing last_active
        self.assertEqual(lease_lib.classify(None, now=NOW), "stale")

    # [AC] INV classification edge case: valid parse round-trips
    def test_parse_claim_valid(self):
        raw = '{"owner":"s1","last_active":"2026-06-14T12:00:00+00:00","pid":42}'
        parsed = lease_lib.parse_claim(raw)
        self.assertIsNotNone(parsed)
        self.assertEqual(parsed["owner"], "s1")
        self.assertEqual(parsed["pid"], 42)

    # [AC] INV edge: unparseable last_active timestamp + dead pid -> stale (not a crash)
    def test_unparseable_timestamp_dead_pid_is_stale(self):
        claim = {"owner": "s1", "last_active": "not-a-timestamp", "pid": 999}
        self.assertEqual(
            lease_lib.classify(claim, now=NOW, pid_alive_fn=lambda p: False), "stale")

    # [AC] INV edge: Z-suffixed UTC timestamp within TTL -> fresh
    def test_z_suffix_timestamp_within_ttl_is_fresh(self):
        claim = {"owner": "s1", "last_active": "2026-06-14T11:55:00Z", "pid": None}
        self.assertEqual(
            lease_lib.classify(claim, now=NOW, pid_alive_fn=lambda p: False), "fresh")

    def test_ttl_constant_is_1800(self):
        self.assertEqual(lease_lib.LEASE_TTL_SECONDS, 1800)


class TestPidAlive(unittest.TestCase):
    # Direct test of the REAL pid_alive (exercises Windows tasklist / POSIX os.kill path).
    def test_own_pid_is_alive(self):
        self.assertTrue(lease_lib.pid_alive(os.getpid()))

    def test_large_unused_pid_is_dead(self):
        # A very large PID is almost certainly not a running process on this host.
        self.assertFalse(lease_lib.pid_alive(2 ** 31 - 1))

    def test_none_pid_is_dead(self):
        self.assertFalse(lease_lib.pid_alive(None))

    def test_invalid_pid_is_dead(self):
        self.assertFalse(lease_lib.pid_alive("notapid"))
        self.assertFalse(lease_lib.pid_alive(0))
        self.assertFalse(lease_lib.pid_alive(-1))


class TestBuildHeartbeatClaim(unittest.TestCase):
    """T4 pure logic: heartbeat claim construction (AC-3.4 parent-keying, AC-4.3 pointer)."""

    def test_keys_owner_off_supplied_session_id(self):  # AC-3.4
        claim = lease_lib.build_heartbeat_claim("parent1", 4321, iso(NOW))
        self.assertEqual(claim["owner"], "parent1")
        self.assertEqual(claim["pid"], 4321)
        self.assertEqual(claim["last_active"], iso(NOW))

    def test_preserves_existing_resume_pointer(self):  # AC-4.3
        existing = {"owner": "parent1", "resume_pointer": "finish step 3"}
        claim = lease_lib.build_heartbeat_claim("parent1", 1, iso(NOW), existing_claim=existing)
        self.assertEqual(claim["resume_pointer"], "finish step 3")

    def test_no_existing_claim_yields_empty_pointer(self):
        claim = lease_lib.build_heartbeat_claim("s1", 1, iso(NOW), existing_claim=None)
        self.assertEqual(claim["resume_pointer"], "")

    def test_existing_without_pointer_field_yields_empty(self):
        claim = lease_lib.build_heartbeat_claim("s1", 1, iso(NOW), existing_claim={"owner": "s1"})
        self.assertEqual(claim["resume_pointer"], "")


class TestReclaimDecision(unittest.TestCase):
    """T6 pure logic: single-winner re-read decision (AC-2.4 / INV-3)."""

    def test_reread_owner_is_us_means_reclaimed(self):  # AC-2.4 winner
        self.assertEqual(
            lease_lib.reclaim_decision("me", {"owner": "me", "pid": 1}), "reclaimed")

    def test_reread_owner_is_other_means_lost(self):  # AC-2.4 loser
        self.assertEqual(
            lease_lib.reclaim_decision("me", {"owner": "rival", "pid": 2}), "lost")

    def test_reread_none_means_lost(self):
        self.assertEqual(lease_lib.reclaim_decision("me", None), "lost")

    def test_reread_corrupt_dict_without_owner_means_lost(self):
        self.assertEqual(lease_lib.reclaim_decision("me", {"pid": 9}), "lost")


class TestLocationFields(unittest.TestCase):
    """T11 pure logic: branch/worktree/task_id claim fields + git-HEAD parsing."""

    def test_build_claim_carries_location_fields(self):
        claim = lease_lib.build_heartbeat_claim(
            "s1", 1, iso(NOW), branch="develop",
            worktree="C:/wt/feature-x", task_id="T11: Location fields")
        self.assertEqual(claim["branch"], "develop")
        self.assertEqual(claim["worktree"], "C:/wt/feature-x")
        self.assertEqual(claim["task_id"], "T11: Location fields")

    def test_build_claim_location_fields_default_empty(self):
        claim = lease_lib.build_heartbeat_claim("s1", 1, iso(NOW))
        self.assertEqual(claim["branch"], "")
        self.assertEqual(claim["worktree"], "")
        self.assertEqual(claim["task_id"], "")

    def test_parse_git_head_symbolic_ref(self):
        self.assertEqual(lease_lib.parse_git_head("ref: refs/heads/develop\n"), "develop")

    def test_parse_git_head_nested_branch_name(self):
        self.assertEqual(
            lease_lib.parse_git_head("ref: refs/heads/feature/session-x"),
            "feature/session-x")

    def test_parse_git_head_detached_returns_short_hash(self):
        self.assertEqual(
            lease_lib.parse_git_head("0123456789abcdef0123456789abcdef01234567\n"),
            "0123456")

    def test_parse_git_head_garbage_or_empty_is_empty(self):
        self.assertEqual(lease_lib.parse_git_head(""), "")
        self.assertEqual(lease_lib.parse_git_head(None), "")

    def test_parse_gitdir_pointer(self):
        self.assertEqual(
            lease_lib.parse_gitdir("gitdir: C:/repo/.git/worktrees/wt1\n"),
            "C:/repo/.git/worktrees/wt1")

    def test_parse_gitdir_non_pointer_is_empty(self):
        self.assertEqual(lease_lib.parse_gitdir("not a pointer"), "")
        self.assertEqual(lease_lib.parse_gitdir(None), "")

    def test_worktree_value_same_as_project_is_main(self):
        self.assertEqual(lease_lib.worktree_value("C:/repo", "C:/repo"), "main")
        # trailing separators / case (Windows) normalize equal
        self.assertEqual(lease_lib.worktree_value("C:/repo/", "C:/repo"), "main")

    def test_worktree_value_differs_is_cwd(self):
        self.assertEqual(
            lease_lib.worktree_value("C:/repo/.worktrees/x", "C:/repo"),
            "C:/repo/.worktrees/x")

    def test_worktree_value_empty_cwd_is_main(self):
        self.assertEqual(lease_lib.worktree_value("", "C:/repo"), "main")

    def test_format_task_id_from_active_task(self):
        data = {"taskId": "T11", "taskTitle": "Location fields"}
        self.assertEqual(lease_lib.format_task_id(data), "T11: Location fields")

    def test_format_task_id_partial_or_absent(self):
        self.assertEqual(lease_lib.format_task_id({"taskId": "T11"}), "T11")
        self.assertEqual(lease_lib.format_task_id({"taskTitle": "X"}), "X")
        self.assertEqual(lease_lib.format_task_id({}), "")
        self.assertEqual(lease_lib.format_task_id(None), "")


class TestDefaultPointer(unittest.TestCase):
    """T12 pure logic: self-maintaining resume pointer default."""

    def test_defaults_empty_pointer_from_active_task(self):
        active = {"taskId": "T11", "taskTitle": "Location fields",
                  "taskLogFile": "Docs/x/task-log.md"}
        self.assertEqual(
            lease_lib.default_resume_pointer("", active),
            "Docs/x/task-log.md § Checkpoint (task T11: Location fields)")

    def test_never_overwrites_non_empty_pointer(self):
        active = {"taskId": "T11", "taskTitle": "X", "taskLogFile": "log.md"}
        self.assertEqual(lease_lib.default_resume_pointer("keep me", active), "keep me")

    def test_no_active_task_keeps_empty(self):
        self.assertEqual(lease_lib.default_resume_pointer("", None), "")
        self.assertEqual(lease_lib.default_resume_pointer("", {}), "")

    def test_missing_task_log_file_keeps_empty(self):
        self.assertEqual(
            lease_lib.default_resume_pointer("", {"taskId": "T1", "taskTitle": "X"}), "")

    def test_truncates_to_200_chars(self):
        active = {"taskId": "T1", "taskTitle": "y" * 400, "taskLogFile": "log.md"}
        out = lease_lib.default_resume_pointer("", active)
        self.assertEqual(len(out), 200)


class TestLeaseGc(unittest.TestCase):
    """T13 pure logic: stale-lease GC decision (>7 days)."""

    def test_gc_constant(self):
        self.assertEqual(lease_lib.LEASE_GC_SECONDS, 7 * 24 * 3600)

    def test_old_claim_is_gc_able(self):
        claim = {"owner": "s1", "last_active": iso(NOW - timedelta(days=8))}
        self.assertTrue(lease_lib.should_gc(claim, mtime_ts=NOW.timestamp(), now=NOW))

    def test_recent_claim_is_kept(self):
        claim = {"owner": "s1", "last_active": iso(NOW - timedelta(days=6))}
        self.assertFalse(lease_lib.should_gc(claim, mtime_ts=0, now=NOW))

    def test_corrupt_claim_old_mtime_is_gc_able(self):
        old_mtime = (NOW - timedelta(days=8)).timestamp()
        self.assertTrue(lease_lib.should_gc(None, mtime_ts=old_mtime, now=NOW))

    def test_corrupt_claim_recent_mtime_is_kept(self):
        recent = (NOW - timedelta(days=1)).timestamp()
        self.assertFalse(lease_lib.should_gc(None, mtime_ts=recent, now=NOW))

    def test_unparseable_last_active_falls_back_to_mtime(self):
        claim = {"owner": "s1", "last_active": "garbage"}
        old_mtime = (NOW - timedelta(days=8)).timestamp()
        recent = (NOW - timedelta(days=1)).timestamp()
        self.assertTrue(lease_lib.should_gc(claim, mtime_ts=old_mtime, now=NOW))
        self.assertFalse(lease_lib.should_gc(claim, mtime_ts=recent, now=NOW))


if __name__ == "__main__":
    unittest.main()
