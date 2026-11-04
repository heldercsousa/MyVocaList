---
id: move-the-playwright-mcp-token-to-env-local
title: Move the Playwright MCP token to .env.local
status: 💡 Pending
target: 2026-11-03
section: DevCycleCraft
parent: hooks-redesign
goal: The token is plain text in the user settings env block, and .env.local holds a different value.
gate: "Helder: put the working token in .env.local, run load-env.ps1, delete the env block from the user settings, restart."
kind: change
---

# Move the Playwright MCP token to .env.local

The token is plain text in the user settings env block, and .env.local holds a different value.

