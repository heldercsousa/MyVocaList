# hooks-redesign — Task Log

## BUG-083 — SQLite MCP fails to connect at session start (Major) — Done 2026-11-04

Root cause: unpinned `uvx` resolved `mcp 2.3.0`, whose breaking 2.x API removes `Server.list_resources` used by the archived `mcp-server-sqlite 2025.4.25`.
Fix: pin both packages in `.mcp.json` (`uvx --with mcp==1.30.0 mcp-server-sqlite==2025.4.25`), per `mcp-governance.md`.

### Changed files

| File | Change |
|---|---|
| `.mcp.json` | sqlite args: `--with mcp==1.30.0`, `mcp-server-sqlite==2025.4.25` |
| `bugs/2026-11-03-BUG-083-…/README.md` | Diagnosis + Verification tables |

### Verification

| Check | Result |
|---|---|
| Fresh process (`/exit` → `claude`), `/mcp` | sqlite ✓ connected, 6 tools |
| `mcp__sqlite__list_tables` | 8 tables returned (`People`, `Venues`, `Events`, `EventParticipations`, …) |
| Regression test | N/A — config only, no testable layer; the fresh-session `/mcp` check is the manual E2E |
