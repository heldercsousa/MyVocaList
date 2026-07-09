# Database Setup & Schema Synchronization

> **Maintenance Note:** This file documents the current database solution and must be updated whenever the database provider changes (e.g., SQLite → PostgreSQL, SQL Server). See the "Maintenance" section at the bottom.

## Current Database Solution

- **Provider:** SQLite via EF Core 10
- **Schema Management:** EF Core migrations (`.cs` files in `MyVocaList.Infra/Migrations/`)
- **Test Database:** Real SQLite temp files (one per test run) via `TestDbContextFactory`
- **MCP Query Database:** `.claude/MyVocaList.db` (synced automatically after migrations)

---

## Initial Setup (Fresh Clone)

```bash
# 1. Restore dependencies
dotnet restore

# 2. Apply migrations to all configured databases
dotnet ef database update -p MyVocaList.Infra -s MyVocaList

# 3. Sync schema to the MCP query database
dotnet ef database update --connection "Data Source=.claude/MyVocaList.db"

# 4. Verify tests run with current schema
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj
```

---

## Adding a New Migration

When adding new tables, columns, or constraints:

```bash
# 1. Create the migration (applies changes to your local schema)
dotnet ef migrations add MigrationDescription -p MyVocaList.Infra

# 2. Apply migrations to the main database
dotnet ef database update -p MyVocaList.Infra -s MyVocaList

# 3. (Automated) The hook in .claude/settings.json automatically syncs to .claude/MyVocaList.db
#    If the hook doesn't run, manually sync:
dotnet ef database update --connection "Data Source=.claude/MyVocaList.db"

# 4. Verify tests still pass
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj
```

---

## How Tests Guarantee Current Schema

**Location:** `MyVocaList.Tests/Infrastructure/TestDbContextFactory.cs`

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
- Triggers after any `dotnet ef database update` command
- Automatically applies the same migrations to `.claude/MyVocaList.db`

**Manual Sync (if hook doesn't run):**
```bash
dotnet ef database update --connection "Data Source=.claude/MyVocaList.db"
```

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
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj

# Migrations are in sync (no pending migrations)
dotnet ef migrations has-pending-changes -p MyVocaList.Infra --exit-code

# MCP database has current schema (can query it without errors)
dotnet ef database update --connection "Data Source=.claude/MyVocaList.db" --dry-run
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
- `TestDbContextFactory.cs` → update provider and temp DB path
- `.claude/.mcp.json` → update SQLite MCP reference if migrating away from SQLite

**Process:**
1. Update spec in `Docs/Management/` documenting the new database solution
2. Update migration code and config in `MyVocaList.Infra/`
3. Update `TestDbContextFactory.cs` to use the new provider
4. Update `.claude/.mcp.json` if needed (or remove if no longer applicable)
5. Update this file with the new procedures
6. Verify tests pass on a fresh clone
7. Commit all changes together with a clear migration commit message
