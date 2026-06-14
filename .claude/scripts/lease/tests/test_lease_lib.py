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


if __name__ == "__main__":
    unittest.main()
