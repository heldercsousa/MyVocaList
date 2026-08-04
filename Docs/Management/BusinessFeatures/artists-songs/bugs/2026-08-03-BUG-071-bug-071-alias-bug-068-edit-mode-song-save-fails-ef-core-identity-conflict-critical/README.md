---
id: BUG-071
title: "BUG-071 (alias BUG-068): Edit-mode song save fails — EF Core identity conflict (Critical)"
status: 💡 Pending
severity: Critical
target: 2026-08-03
section: BusinessFeatures
parent: artists-songs
goal: editing a saved song must persist; today one tap reports success but writes nothing (silent data loss) and a second tap throws an EF tracking conflict.
gate: Red at the repository/real-SQLite seam first — mocked service tests cannot reproduce it.
kind: bug
---

# BUG-071 (alias BUG-068): Edit-mode song save fails — EF Core identity conflict (Critical)

editing a saved song must persist; today one tap reports success but writes nothing (silent data loss) and a second tap throws an EF tracking conflict.

