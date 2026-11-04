---
id: BUG-083
title: SQLite MCP fails to connect at session start
status: 💡 Pending
severity: Major
target: 2026-11-03
section: DevCycleCraft
parent: hooks-redesign
goal: The sqlite MCP server shows failed at every session start, yet uvx mcp-server-sqlite runs by hand. The MCP Availability Gate needs it before implementation work.
gate: Server connects in a fresh session (/mcp shows connected) and a read-only query returns the schema.
kind: bug
---

# SQLite MCP fails to connect at session start

The sqlite MCP server shows failed at every session start, yet uvx mcp-server-sqlite runs by hand. The MCP Availability Gate needs it before implementation work.


## Evidence (2026-11-03)

- `/mcp` and `/plugins` → `sqlite` **failed** in every fresh session today (earlier: `CONNECTION_CLOSED`).
- `.mcp.json`: `cmd /c uvx mcp-server-sqlite --db-path C:/Users/helde/source/repos/MyVocaList/.claude/MyVocaList.db`;
  enabled in `settings.local.json` `enabledMcpjsonServers`.
- By hand, `cmd //c "uvx mcp-server-sqlite --help"` prints its usage → package resolves. Suspects: startup
  timeout on first `uvx` resolve, stdio wrapping by `cmd /c`, or the db path.
- Governance: `.claude/library/mcp-governance.md` (local stdio only, pin versions — `uvx` is unpinned today).

## Fix checklist

Run `claude --debug` (or `--mcp-debug`) and read the sqlite startup error → fix → fresh session `/mcp` shows connected →
`list_tables` works. Pin the package version (`mcp-server-sqlite==<x>`) per governance. Task-log entry required (Major).
