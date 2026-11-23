# Database Setup & Schema Synchronization

> **Maintenance Note:** This file documents the current database solution and must be updated whenever the database provider changes (e.g., SQLite → PostgreSQL, SQL Server). See the "Maintenance" section at the bottom.

## Current Database Solution

- **Provider:** SQLite via EF Core 10
- **Schema Management:** EF Core migrations (`.cs` files in `Infra/Migrations/`)
- **Test Database:** Real SQLite temp files (one per test run) via `TestDbContextFactory`
- **MCP Query Database:** `.claude/MyVocaList.db` (synced automatically after migrations)

---

## Initial Setup (Fresh Clone)

```bash
# 1. Restore dependencies
dotnet restore

# 2. Sync schema to the MCP query database (the app's own DB lives on the device
#    and is migrated by the app at startup — there is no desktop app DB to update)
dotnet ef database update -p Infra -s Infra --connection "Data Source=<absolute repo path>/.claude/MyVocaList.db"

# 3. Verify tests run with current schema
dotnet test Tests/MyVocaList.Tests.csproj
```

---

## Adding a New Migration

When adding new tables, columns, or constraints:

```bash
# 1. Create the migration
dotnet ef migrations add MigrationDescription -p Infra -s Infra

# 2. Sync the MCP query database (the app migrates the device DB at startup)
dotnet ef database update -p Infra -s Infra --connection "Data Source=<absolute repo path>/.claude/MyVocaList.db"

# 3. Verify tests still pass
dotnet test Tests/MyVocaList.Tests.csproj
```

---

## How Tests Guarantee Current Schema

**Location:** `Tests/Infrastructure/TestDbContextFactory.cs`

```csharp
public static AppDbContext Create()
{
    var dbPath = Path.Combine(Path.GetTempPath(), $"myvocalist_test_{Guid.NewGuid():N}.db");
    
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite($"Data Source={dbPath}")
        .AddInterceptors(new CollationInterceptor())
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
        .Options;

    return new AppDbContext(options);
}
```

**Why this works:**
1. Creates a **real SQLite database** (not in-memory) for each test
2. Uses a unique temp file (`Guid.NewGuid()`) to avoid test collisions
3. Automatically applies current migrations when the `DbContext` is instantiated
4. **No manual schema sync needed for tests** — they always run with the latest schema

**Result:** Tests pass on any machine (fresh clone, CI/CD, parallel test runs) without setup.

---

## SQLite MCP Database Synchronization

**Location:** `.claude/MyVocaList.db`

**Auto-sync Mechanism:**
- Configured in `.claude/settings.json` → `PostToolUse` hook
- Triggers after any `dotnet ef database update` command and runs `.claude/scripts/sync-mcp-db.ps1`
- The script applies the migrations to `.claude/MyVocaList.db` and prints `SQLite MCP DB synced…` or `SQLite MCP DB sync FAILED…` (it never blocks)
- Running the manual command below also fires the hook; the second run is a no-op

**Manual Sync (if the hook doesn't run):**
```bash
dotnet ef database update -p Infra -s Infra --connection "Data Source=<absolute repo path>/.claude/MyVocaList.db"
```

**Why `-p Infra -s Infra` and an absolute path:**
- The app project (`MyVocaList`) cannot be the EF startup project: it targets `net10.0-android` and has no desktop `deps.json`. `Infra` has a design-time factory (`Infra/AppDbContextFactory.cs`).
- EF resolves a relative `Data Source` against the startup project folder (`Infra/`), so `.claude/MyVocaList.db` fails with `SQLite Error 14: unable to open database file`.
- The database uses the `NOCASE_NOACCENT` collation, which the SQLite MCP server does not register: queries touching collated columns fail there with `no such collation sequence`. For such queries use Python `sqlite3` with `create_collation('NOCASE_NOACCENT', …)`.

**Why MCP needs sync:**
- Claude Code's SQLite MCP queries `.claude/MyVocaList.db` for schema inspection
- If this DB has an old schema, MCP queries fail or return wrong results
- Auto-sync ensures the MCP always sees the current schema

---

## Verification Checklist

Run this after any migration changes:

```bash
# Build succeeds with 0 errors
dotnet build

# Tests pass (uses current schema via TestDbContextFactory)
dotnet test Tests/MyVocaList.Tests.csproj

# The model has no changes missing a migration
dotnet ef migrations has-pending-model-changes -p Infra -s Infra

# MCP database has every migration applied (pending ones are marked "(Pending)")
dotnet ef migrations list -p Infra -s Infra --connection "Data Source=<absolute repo path>/.claude/MyVocaList.db"
```

---

## Maintenance: When Database Solution Changes

**If migrating to a different database provider (e.g., PostgreSQL, SQL Server):**

This file **must** be updated to reflect:
1. New provider name and version
2. New connection string format
3. New migration command syntax (if different)
4. New test database setup (if using containers, Docker, etc.)
5. New MCP sync mechanism (if applicable)
6. Any new verification checklist items

**Files to update simultaneously:**
- `.claude/library/database-setup.md` (this file) — PRIMARY AUTHORITY
- `README.md` → keep link but no DB-specific details
- `.claude/settings.json` → update PostToolUse hook if needed
- `.claude/scripts/sync-mcp-db.ps1` → the command the hook runs
- `TestDbContextFactory.cs` → update provider and temp DB path
- `.claude/.mcp.json` → update SQLite MCP reference if migrating away from SQLite

**Process:**
1. Update spec in `Docs/Management/` documenting the new database solution
2. Update migration code and config in `Infra/`
3. Update `TestDbContextFactory.cs` to use the new provider
4. Update `.claude/.mcp.json` if needed (or remove if no longer applicable)
5. Update this file with the new procedures
6. Verify tests pass on a fresh clone
7. Commit all changes together with a clear migration commit message
