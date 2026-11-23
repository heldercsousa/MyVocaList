---
id: move-the-playwright-mcp-token-to-env-local
title: Move the Playwright MCP token to .env.local
status: 🔵 Superseded
target: —
section: DevCycleCraft
parent: hooks-redesign
goal: The token is plain text in the user settings env block, and .env.local holds a different value.
gate: "Helder: put the working token in .env.local, run load-env.ps1, delete the env block from the user settings, restart."
closed: 2026-11
kind: change
---

# Move the Playwright MCP token to .env.local

The token is plain text in the user settings env block, and .env.local holds a different value.


## Note (2026-11-19)

Helder dropped this item from the backlog. The token now lives in the persistent user environment variable `PLAYWRIGHT_MCP_EXTENSION_TOKEN`, `.env.local` is untracked and ignored, and Helder rotates the old token himself. Claude will not raise it again.
