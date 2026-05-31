# Backup & Restore — Task Log

---

## Task: Phase 1, Task 1 — Domain entities, enums, and service interfaces
**Plan:** Docs/Management/BusinessFeatures/backup-restore/plan.md
**Status:** To Review
**Started:** 05/30/2026
**Completed:** 05/30/2026

### Changed files:
- `Domain/Entity/BackupTrigger.cs` — created BackupTrigger enum (AppStop, QueueCreated, RoundCompleted, QueueClosed, Manual)
- `Domain/Entity/BackupType.cs` — created BackupType enum (FullSnapshot, TransactionLog)
- `Domain/Entity/MirrorStatus.cs` — created MirrorStatus enum (NotAttempted, Pending, Confirmed)
- `Domain/Entity/BackupHistory.cs` — created BackupHistory POCO entity
- `Domain/RepositoryInterface/IBackupRepository.cs` — created IBackupRepository interface
- `Domain/ServicesInterfaces/ITransactionLogWriter.cs` — created ITransactionLogWriter interface + LogEntry record
- `Domain/ServicesInterfaces/IBackupService.cs` — created IBackupService interface + BackupResult record

### Verification evidence
- Build: PASS — `dotnet build Domain/MyVocaList.Domain.csproj` → 0 errors, 0 warnings
- Tests: SKIPPED (Domain entities are Level C per testing.md — pure POCOs/interfaces with no logic)
- Post-edit re-read: confirmed — all 7 files match plan spec exactly
- Spec compliance: confirmed — plan.md Phase 1 Task 1 steps 1–5 implemented in full

### AC traceability
N/A — Domain entities are Level C; no user-facing ACs assigned to this task.

---

## Task: Phase 2, Tasks 2–4 — EF config, migration, BackupRepository, TransactionLogInterceptor
**Plan:** Docs/Management/BusinessFeatures/backup-restore/plan.md
**Status:** To Review
**Started:** 05/31/2026
**Completed:** 05/31/2026

### Changed files:
- `Infra/EntityEFConfig/BackupHistoryConfiguration.cs` — created EF fluent config (HasConversion<string> for all enum properties, HasIndex on CreatedAt)
- `Infra/AppDbContext.cs` — added `DbSet<BackupHistory> BackupHistories` + `ApplyConfiguration(new BackupHistoryConfiguration())`
- `Infra/Migrations/20260531044743_AddBackupHistory.cs` — generated migration (CreateTable BackupHistories, CreateIndex on CreatedAt)
- `Infra/Migrations/20260531044743_AddBackupHistory.Designer.cs` — generated migration snapshot
- `Infra/Migrations/AppDbContextModelSnapshot.cs` — updated model snapshot (auto-generated)
- `Infra/Repository/BackupRepository.cs` — created BackupRepository implementing IBackupRepository
- `Infra/Interceptor/TransactionLogInterceptor.cs` — created SaveChangesInterceptor capturing before/after write state; BackupHistory writes excluded to prevent infinite loop
- `MyVocaList.Tests/Integration/Repositories/BackupRepositoryTests.cs` — created 4 integration tests (TDD Red → Green confirmed)

### Build notes
- Infra build: PASS — `dotnet build Infra/MyVocaList.Infra.csproj` → 0 errors, 0 warnings
- EF tools version warning: EF tools 9.0.6 vs runtime 10.0.0 — pre-existing, not introduced by this task
- Solution-wide build: pre-existing nullable test errors in SongKaraokeUrlRepositoryTests, PersonServiceTests, SongRepositoryTests — not introduced by this task

### Verification evidence
- Build: PASS — Infra project 0 errors, 0 warnings
- Tests: PASS — 199 total (4 new BackupRepositoryTests all green, 195 pre-existing all still passing)
- Post-edit re-read: confirmed — all 8 files match plan spec
- Spec compliance: confirmed — plan.md Phase 2 Tasks 2, 3, 4 implemented in full

### AC traceability
| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|------------------------|-------------|
| (BackupHistory table) | AddAsync persists entry | BackupRepository.AddAsync | AddAsync_ValidEntry_PersistedAndReturnedByGetRecent |
| (GetRecentAsync order) | Returns entries desc by CreatedAt | BackupRepository.GetRecentAsync | GetRecentAsync_MultipleEntries_ReturnsOrderedByCreatedAtDesc |
| (GetRecentAsync limit) | Respects limit parameter | BackupRepository.GetRecentAsync | GetRecentAsync_LimitApplied_ReturnsOnlyRequestedCount |
| (GetLatestSnapshot filter) | Returns only FullSnapshot type | BackupRepository.GetLatestSnapshotAsync | GetLatestSnapshotAsync_MixedTypes_ReturnsOnlySnapshot |

---

## Task: Phase 3, Tasks 5–6 — TransactionLogWriter + BackupService + unit tests
**Plan:** Docs/Management/BusinessFeatures/backup-restore/plan.md
**Status:** To Review
**Started:** 05/31/2026
**Completed:** 05/31/2026

### Changed files:
- `Services/TransactionLogWriter.cs` — created TransactionLogWriter implementing ITransactionLogWriter; file-based JSONL log with SemaphoreSlim concurrency guard and prune-by-timestamp
- `Services/BackupService.cs` — created BackupService implementing IBackupService; constructor `(IBackupRepository, ITransactionLogWriter, ILogger<BackupService>, string dbPath, string backupDir)`; `#if ANDROID` ShareFileAsync block compiles as dead code in net10.0 TFM (correct)
- `MyVocaList.Tests/Unit/Services/BackupServiceTests.cs` — created 6 unit tests: 2 for TransactionLogWriter, 4 for BackupService (TDD Red → Green confirmed)
- `MyVocaList.sln` — added 3 new files to backup-restore solution folder GUID {FA1234BC-0001-4000-8000-000000000011}

### Build notes
- Solution-wide build: PASS — 0 errors (warnings are pre-existing)
- `#if ANDROID` block in BackupService.ShareFileAsync: compiles as dead code in Services (net10.0) — expected per platform note in task spec

### Verification evidence
- Build: PASS — `dotnet build MyVocaList.sln` → 0 errors
- Tests: PASS — 205 total (6 new BackupServiceTests all green, 199 pre-existing all still passing)
- Post-edit re-read: confirmed — TransactionLogWriter.cs, BackupService.cs, BackupServiceTests.cs match plan spec
- Spec compliance: confirmed — plan.md Phase 3 Tasks 5 and 6 implemented in full

### AC traceability
| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|------------------------|-------------|
| (AppendAsync writes JSONL) | JSON line written with op/entity fields | TransactionLogWriter.AppendAsync | AppendAsync_SingleEntry_WritesJsonLineToFile |
| (PruneLogsOlderThan deletes old) | Log files older than snapshot deleted | TransactionLogWriter.PruneLogsOlderThanAsync | PruneLogsOlderThanAsync_OldFile_Deleted |
| (CreateFullBackupAsync creates file) | Snapshot file created and history recorded | BackupService.CreateFullBackupAsync | CreateFullBackupAsync_ValidDb_CreatesFileAndRecordsHistory |
| (GetHistoryAsync delegates) | Returns repo results with limit | BackupService.GetHistoryAsync | GetHistoryAsync_ReturnsRepositoryResult |
| (HasRecentBackup true) | Returns true within 24h | BackupService.HasRecentBackupAsync | HasRecentBackupAsync_RecentSnapshot_ReturnsTrue |
| (HasRecentBackup false) | Returns false when no snapshot | BackupService.HasRecentBackupAsync | HasRecentBackupAsync_NoSnapshot_ReturnsFalse |
