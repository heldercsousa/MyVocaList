---
id: BUG-073
title: RestoreFromBundleAsync overwrites the live SQLite file while AppDbContext holds it open
status: 💡 Pending
severity: Major
target: 2026-08-04
section: BusinessFeatures
parent: backup-restore
goal: "BackupService.RestoreFromBundleAsync:137 does File.Copy(snapshotFile, _dbPath, overwrite: true) on the same path AppDbContext's connection string points at, with no context dispose and no connection close."
gate: Found during UOW plan verification 2026-08-04; unrelated to the unit-of-work rollout and deliberately excluded from it.
kind: bug
---

# RestoreFromBundleAsync overwrites the live SQLite file while AppDbContext holds it open

BackupService.RestoreFromBundleAsync:137 does File.Copy(snapshotFile, _dbPath, overwrite: true) on the same path AppDbContext's connection string points at, with no context dispose and no connection close.

