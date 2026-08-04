---
id: merge-duplicate-repository-families-into-one-infra-repository-infra-repositories
title: Merge duplicate repository families into one (Infra/Repository + Infra/Repositories)
status: 💡 Pending
target: 2026-08-04
section: DevCycleCraft
parent: read-model-notracking-guidelines
goal: Two repository folders exist by accident from prior sessions; they must become one family so later refactors touch a single code path.
gate: Prerequisite for the DbContext unit-of-work waves; those waves must not start until the merge lands.
kind: change
---

# Merge duplicate repository families into one (Infra/Repository + Infra/Repositories)

Two repository folders exist by accident from prior sessions; they must become one family so later refactors touch a single code path.

