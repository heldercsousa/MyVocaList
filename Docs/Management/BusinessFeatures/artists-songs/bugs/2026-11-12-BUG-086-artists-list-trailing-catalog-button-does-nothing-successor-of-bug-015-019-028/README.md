---
id: BUG-086
title: Artists list trailing catalog button does nothing (successor of BUG-015/019/028)
status: 🟡 In Progress
severity: Major
target: 2026-11-12
section: BusinessFeatures
parent: artists-songs
goal: "Tapping the trailing catalog button on an artist row must open that artist's catalog. Suspected cause: fire-and-forget Shell GoToAsync to the top-level songs flyout route, which the rest of the app reaches by PushAsync plus manual query-apply."
gate: Diagnosis to confirm before the fix; regression test or documented on-device check; Helder passes it on device.
kind: bug
---

# Artists list trailing catalog button does nothing (successor of BUG-015/019/028)

Tapping the trailing catalog button on an artist row must open that artist's catalog. Suspected cause: fire-and-forget Shell GoToAsync to the top-level songs flyout route, which the rest of the app reaches by PushAsync plus manual query-apply.
