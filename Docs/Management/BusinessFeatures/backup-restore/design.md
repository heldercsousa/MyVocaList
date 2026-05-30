# Design — Data Backup & Restore

**Feature:** Data Backup & Restore
**Status:** Design (pre-spec — no tasks.md yet)
**Created:** 2026-05-30
**Scope:** MVP (Tier 1 + Tier 3) · MVP-stretch (Tier 2) · Post-MVP (cloud sync, companion agent, log delta restore)

---

## Problem Statement

MyVocaList is used at live events where a device failure mid-event means losing queue state, singer
history, and catalog data. A user whose device is stolen, broken, or drained during an event needs
to recover on a new device in minutes — not hours. Beyond the event scenario, any user who replaces
their device should not lose months of catalog and queue history.

---

## Key Scenarios (ordered by urgency)

| # | Scenario | Recovery time target | Tier that covers it |
|---|----------|----------------------|---------------------|
| S1 | Device fails mid-event, spare phone available on same WiFi | < 2 minutes | Tier 2 |
| S2 | Device fails mid-event, no spare — but backup was shared before event | < 5 minutes | Tier 3 |
| S3 | User gets a new phone (planned) | Next day, no rush | Tier 1 + Tier 3 |
| S4 | App crash / data corruption on same device | Immediate | Tier 1 |

---

## Three-Tier Architecture

```
┌─────────────────────────────────────────────────────────┐
│  Tier 3 — Manual Export (always available)              │
│  Share sheet · Pre-event advisory                       │
├─────────────────────────────────────────────────────────┤
│  Tier 2 — WiFi Mirror (opt-in, event safety net)        │
│  mDNS discovery · Pairing code · Encrypted stream       │
├─────────────────────────────────────────────────────────┤
│  Tier 1 — Local Backup (unconditional, always on)       │
│  SQLite snapshot · Transaction log · Backup history     │
└─────────────────────────────────────────────────────────┘
```

Tiers are independent. A user who never configures Tier 2 still has Tier 1 and Tier 3.

---

## Tier 1 — Local Backup

### 1.1 Full Backup (SQLite Snapshot)

The entire SQLite database file is copied to the app's private internal storage
(`Context.FilesDir` on Android). No external storage permission required.

**Trigger events:**

| Trigger | Rationale |
|---------|-----------|
| App stop (`Window.Stopped`) | Lifecycle — app backgrounded |
| Queue created | High-value moment — catalog + new queue state |
| Round completed | Queue progression captured |
| Queue closed | Event completed — most complete state |
| Manual (user-initiated) | On-demand from Settings |

**Naming convention:** `backup_YYYYMMDD_HHmmss.db`

**Retention policy:** Keep last 10 snapshots. Oldest deleted when limit reached.

### 1.2 Transaction Log

An append-only log of every write operation persisted to the database. Written
immediately after each successful EF Core `SaveChanges`. One file per app session
(new session = new file at app start).

**Dual purpose:**
1. **Restore from log** — on a fresh device, after the snapshot is applied, only log entries
   with `ts` later than the snapshot timestamp are applied to fill the gap between the
   last backup and the moment of failure. Entries already covered by the snapshot are ignored.
2. **Undo source** — apply log entries backward (inverting each operation) to power in-app undo.
   This gives the app a consistent undo backbone across all features, superseding the
   per-feature commit-first snackbar pattern (see BACKLOG.md — Inline Undo Pattern UX Standard).

**Entry schema (JSON lines format):**

```json
{ "ts": "2026-05-30T21:14:03Z", "op": "Create", "entity": "Singer", "id": 42, "before": null, "after": { "name": "João Silva" } }
{ "ts": "2026-05-30T21:15:10Z", "op": "Update", "entity": "QueueEntry", "id": 7, "before": { "status": "Waiting" }, "after": { "status": "Sung" } }
{ "ts": "2026-05-30T21:16:00Z", "op": "Delete", "entity": "QueueEntry", "id": 3, "before": { "name": "Maria" }, "after": null }
```

**Fields:** `ts` (UTC ISO-8601), `op` (Create/Update/Delete), `entity`, `id`,
`before` (state before the operation — null for Create), `after` (state after — null for Delete).

**Undo inverse mapping:**

| Original op | Undo op | Data used |
|-------------|---------|-----------|
| Create | Delete | `after.id` |
| Update | Update | `before` values |
| Delete | Create | `before` values |

**Storage:** `logs/session_YYYYMMDD_HHmmss.jsonl` in internal storage.

**Retention policy:** After each successful full backup, delete all session log files
whose last entry `ts` is earlier than the snapshot timestamp — those entries are already
captured in the snapshot and no longer needed for restore. Log files that straddle the
boundary (entries both before and after the snapshot) are kept in full for simplicity.
Maximum 30 session log files retained at any time as a hard cap.

**Mirror delivery status per entry:** Each entry carries a delivery flag used by Tier 2:
`pending` | `sent` | `acknowledged`. Entries not yet acknowledged are re-sent when the
mirror reconnects.

### 1.3 Backup History

A lightweight local table (`BackupHistory`) records every backup event for display
in Settings and for the pre-event advisory.

```
BackupHistory
─────────────
Id              int  PK
CreatedAt       datetime
TriggerType     enum  (AppStop | QueueCreated | RoundCompleted | QueueClosed | Manual)
BackupType      enum  (FullSnapshot | TransactionLog)
FilePath        string   (internal path)
FileSizeBytes   long
MirrorStatus    enum  (NotAttempted | Pending | Confirmed)
```

---

## Tier 2 — WiFi Mirror

### 2.1 Overview

During an active event, the host device advertises a MyVocaList mirror service on the
local WiFi via mDNS. Any other device on the same network running MyVocaList can
discover it, pair with it, and receive transaction log entries in real time.

If the host device fails, the paired device has a near-complete log. A fresh MyVocaList
install on a new device can discover a mirror on the same WiFi and restore from it
in one tap.

### 2.2 mDNS Discovery

Service type: `_myvocalist-mirror._tcp.local`

Host advertises:
- Service name: device-friendly name (e.g. "João's Phone")
- Port: configurable, default `47731`
- TXT record: `version`, `venueId` (to match the active event)

A fresh install scans for `_myvocalist-mirror._tcp.local` on startup. If a mirror is
found for the active venue, it surfaces a restore prompt automatically.

**Library:** `Zeroconf` NuGet package (cross-platform mDNS for .NET).

### 2.3 Security — Pairing Code

The WiFi network may be shared (venue guests, public hotspot). Log entries contain
personal information (singer names, queue history). The mirror stream must be
encrypted.

**Pairing flow:**

1. Host generates a random 6-digit pairing code and displays it on screen.
2. User on the receiver device enters the code.
3. Both sides derive an AES-256 session key from the code + a random salt
   exchanged during the TCP handshake (PBKDF2, 100,000 iterations).
4. All subsequent log entry transmission is encrypted with this key.
5. Pairing code expires after 5 minutes or after first successful pairing.

**Threat model covered:** Passive eavesdropping on shared WiFi networks.
**Not covered:** A malicious actor who physically observes the 6-digit code being
entered — considered out of scope for this threat model.

### 2.4 Sync Protocol

- Transport: TCP (reliable, ordered delivery).
- Host pushes new log entries as they are written.
- Receiver sends ACK per entry. Host marks entry as `acknowledged`.
- If receiver goes offline, host queues `pending` entries in memory (bounded to
  last 500 entries or 1 MB, whichever comes first — exact limit TBD at
  implementation). On reconnect, pending entries are flushed before new ones.
- Receiver stores the full log in its own internal storage.

### 2.5 Restore from Mirror

On a fresh device:

1. User opens MyVocaList → app scans for mirror services on local WiFi.
2. If found: "Mirror found — João's Phone · last sync 2 minutes ago. Restore?"
3. User confirms. App downloads full log + latest snapshot from mirror.
4. App applies snapshot, confirms integrity, shows success.

### 2.6 Configuration

Tier 2 is opt-in. Settings > Backup > WiFi Mirror:
- Toggle: Enable mirror host / Enable mirror receiver
- Paired devices list (manage active pairings)
- Mirror status: active / offline / not configured

---

## Tier 3 — Manual Export

### 3.1 Share Sheet Export

Available at any time from Settings > Backup > Export Now.

Exports a zip bundle containing:
- Latest full snapshot (`backup_YYYYMMDD_HHmmss.db`)
- All session log files since the snapshot (`logs/session_*.jsonl`)

User invokes Android share sheet and sends to any target (email, WhatsApp, Google
Drive, USB, etc.). The receiving end is responsible for storage — MyVocaList has
no knowledge of where the bundle landed.

**Restore from export:** Settings > Backup > Restore from File → Android file picker
(SAF). User selects the exported zip. App validates, restores snapshot, confirms.

### 3.2 Pre-Event Advisory

When a queue is created, the app checks: "Has a backup been exported or mirrored
in the last 24 hours?"

If not: a non-blocking snackbar advisory appears:
> "Heading to an event? Consider backing up first." [Back up now]

Advisory is dismissible and does not block queue creation.

---

## Settings UI — Backup Section

Settings > Backup:

```
Last backup: Today 21:14 — Auto (Queue created)    [Back up now]

─── Auto-Backup ───────────────────────────────────
Auto-backup                          [ON]
Triggers: App close, Queue events    [Configure]

─── WiFi Mirror ───────────────────────────────────
Mirror host                          [OFF]
Mirror receiver                      [OFF]
Paired devices                       [Manage →]

─── Export & Restore ──────────────────────────────
Export backup                        [Share →]
Restore from file                    [Choose file →]

─── History ───────────────────────────────────────
                                     [View all →]
  Today 21:14  Auto · Queue created  ✓ Mirror confirmed
  Today 20:01  Auto · App stop       ○ Mirror not configured
  Yesterday    Manual                ✓ Mirror confirmed
```

---

## Layers Affected

| Layer | Change |
|-------|--------|
| Domain | `BackupHistory` entity; `IBackupRepository`; `IMirrorSyncService` interface |
| Infra | `BackupRepository`; EF migration for `BackupHistory`; `BackupEngine` (file I/O); `MirrorSyncService` (TCP + mDNS + crypto) |
| Services | `BackupService` (orchestrates triggers, history, export); `MirrorHostService`; `MirrorReceiverService` |
| MAUI | `BackupSettingsPage`; `BackupSettingsViewModel`; lifecycle hooks in `App.cs` (`Window.Stopped`); domain event hooks in queue services |
| Android | `FileProvider` for share sheet; mDNS via Zeroconf; TCP server on background thread |

---

## Interface Sketches

```csharp
public interface IBackupService
{
    Task<BackupResult> CreateFullBackupAsync(BackupTrigger trigger, CancellationToken ct);
    Task AppendTransactionLogEntryAsync(LogEntry entry, CancellationToken ct);
    Task<(bool success, string message)> ExportBundleAsync(CancellationToken ct);
    Task<(bool success, string message)> RestoreFromBundleAsync(string zipPath, CancellationToken ct);
    Task<IReadOnlyList<BackupHistoryDto>> GetHistoryAsync(int limit, CancellationToken ct);
}

public interface IMirrorHostService
{
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    bool IsRunning { get; }
    int PendingEntryCount { get; }
}

public interface IMirrorReceiverService
{
    Task<IReadOnlyList<MirrorServiceInfo>> DiscoverAsync(TimeSpan timeout, CancellationToken ct);
    Task<(bool success, string message)> PairAndRestoreAsync(MirrorServiceInfo mirror, string pairingCode, CancellationToken ct);
}

public enum BackupTrigger
{
    AppStop, QueueCreated, RoundCompleted, QueueClosed, Manual
}
```

---

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Backup format | SQLite file copy | Atomic, complete, no serialization logic |
| Transaction log format | JSON lines (`.jsonl`) | Append-friendly, human-readable, simple to parse |
| Log granularity | Per write operation | Maximum recovery granularity |
| Log file split | Per session | Bounded file size; easy to identify by event |
| Mirror transport | TCP | Reliable ordered delivery required for log integrity |
| Mirror discovery | mDNS (Zeroconf) | Zero-config, no IP setup, works offline |
| Mirror encryption | AES-256 + pairing code + PBKDF2 | Protects personal data on shared WiFi; low UX friction |
| Restore from log (MVP) | Implemented — delta only | Only log entries with `ts` after snapshot timestamp applied; entries already in snapshot are skipped |
| Cloud sync | Post-MVP | Adds auth infrastructure before MVP is validated |
| Companion agent app | Post-MVP | High value but separate deliverable |

---

## Post-MVP Roadmap

1. **In-app undo history** — UI surface for the transaction log's undo capability; multi-step undo across the whole app, replacing per-feature snackbar patterns
2. **Cloud sync** — Google Drive OAuth; silent background push of snapshots + logs
3. **Companion agent** — lightweight Windows / Android background app; auto-discovered via mDNS; no UI required on receiver
4. **Differential backup** — add `UpdatedAt` to all entities; export only changed records since last backup

---

## Out of Scope (MVP)

- Internet-based sync of any kind
- In-app undo history UI (log is written; UI surface is post-MVP)
- Companion agent app
- Encryption of the local backup files at rest (internal private storage is already sandboxed by Android)
- Backup of app settings / preferences (catalog + queue data only)
