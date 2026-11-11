# Task Log — CRUD list selection gestures

## 2026-11-11 — Spec

- MD3 docs rendered from m3.material.io (Playwright 1.x from the npx cache + installed Edge; Playwright MCP not configured in this session). Evidence quoted in `design.md § MD3 evidence`.
- Context7 (`/websites/devexpress_maui`): `DXCollectionView.Tap` / `LongPress` (`CollectionViewGestureEventArgs.Item`, `ItemHandle`); no tap-selection suppression → dynamic `SelectionMode` design.
- Spec written: README, requirements, design, tasks, plan.
- Spec relocated to the post-ship `changes/` folder of `crud-list-deduplication` (shipped spec immutability); registered via `backlog_gen.py register --kind change`; `floatingtoolbar-visibility-verify` → 🔵 Superseded.
- Review round 1 (fresh `spec-reviewer` + `plan-reviewer`): both FAIL. Blocking items: interface signatures, ancestor resolution, mode-switch race, AC testability, T1 build-green/ownership, Delete slot move, deferred decisions, governed-gate artifacts, T2 sizing. All folded into rev. 2.
