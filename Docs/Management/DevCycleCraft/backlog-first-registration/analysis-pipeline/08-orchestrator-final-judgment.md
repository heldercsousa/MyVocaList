# 08 — Orchestrator's Final Judgment (the user-reserved decision)

**Question (reserved by the user for the orchestrator alone):** which of the two Wave-4 reviews is
stronger, judged against the project's current settings?

## Decision
**Reviewer 1 is the stronger review overall — BUT Reviewer 2 contributes two unique, must-incorporate
correctness catches that R1 missed. Downstream action: adopt R1 as the primary review and fold in
R2's two unique findings.** (Both findings are already merged into `09-final-consolidated-plan.md`.)

Both reviewers independently returned **Approve with changes**, both verified claims against live
config with line citations, and both converged on the same #1 issue (the plan builds the advisory
deliverable while the enforcement *posture* is still open, though BACKLOG line 150 literally says
"block session end"). The tie-breaker is which review's *unique* catches matter more under the
project's current settings, which prize: mechanically-enforced HARD GATES, feasibility against the
real hook system, and the non-negotiable that legitimate memory use is never flagged.

## Why R1 ranks first (settings-grounded severity)
1. **`.sln` HARD GATE miss (constitutional).** R1 caught that the new `.claude/scripts/backlog/*.py`
   files are NOT covered by `sync-docs-to-sln.ps1` (acts only on `Docs\` paths, line 28, only on
   `Write`, line 71). `constraints-registry.md § Visual Studio Solution` makes `.sln` registration of
   any new `.claude/` file a HARD GATE; the plan's AC-9 only registered spec `.md` files — a true gate
   violation. R2 only flagged the weaker Write-vs-Edit nuance for `.md` files and implied the
   auto-writer covers it; R2 missed that the `.py` files are entirely outside the sync hook's scope.
2. **Hook-architecture feasibility.** R1 caught that Stop is an `agent`-type hook (a natural-language
   prompt), so "weave `orphan_check.py` into the agent prompt" is non-deterministic; the robust
   pattern — grounded in the live lease precedent (`heartbeat.py` is a separate command-type entry
   under the existing `Stop` key) — is a command-type entry. Exactly the "feasibility against the real
   hook system" the settings demand. R2 did not catch this.
3. **Changelog-collision.** R1 caught that `TaskCompleted`/`Stop` agent hooks auto-touch
   `changelog.md`, so the `amend:` changelog triple must stay in `proposed-diffs.md` (un-applied)
   rather than be written to `changelog.md` — another hook-aware, settings-grounded catch R2 missed.

## Why R2 must still be folded in (two unique correctness holes R1 missed)
1. **`MEMORY.md` is agent-curated, not purely harness-automatic.** R2 verified `MEMORY.md` carries
   hand-written "Active Feature" pointers. The plan's exempt **category 4** ("harness-AUTOMATIC
   captures the agent did not author") would blanket-exempt `MEMORY.md` and blind the very mechanism
   the feature exists to build if an agent adds a new-work line to `MEMORY.md` itself. The classifier
   must work at **line/content level**, never as a whole-file exemption. A genuine design hole R1
   did not surface.
2. **Classifier signal-precedence.** R2 caught that a `project_*` pointer reading
   "NEXT: implement the X service" carries BOTH an exempt resume marker AND a new-work verb; the plan
   states no precedence on conflict, so AC-11 ("never flag legitimate use") is asserted, not proven.
   The adversarial conflict cases must be enumerated in the test matrix. R1 did not surface this.
   (R2 also adds the inherited-STEP-5 coarse-correlation false-suppression note — a worthwhile
   documented limitation.)

## Net
R1 is the stronger standalone review (more, and more severe, HARD-GATE/feasibility catches that map
to the project's mechanically-enforced constraints). `09-final-consolidated-plan.md` adopts **all of
R1's 8 required changes plus R2's two unique catches** (MEMORY.md line-level classification; classifier
signal-precedence + adversarial tests) and records R2's coarse-correlation limitation.
