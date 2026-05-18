# Conflict Report — workflow.md Internal Analysis
**Date:** 2026-05-06
**Analyst:** Agent A
**Scope:** workflow.md internal — duplications, contradictions, inconsistencies, structural issues

---

## Summary
**14 duplications, 5 contradictions, 6 inconsistencies, 8 structural issues**

---

## Duplications

### D-01: "Re-read spec before briefing subagent" stated in three places
- **Location 1:** "Mandatory spec reads at session start" (under Rule 2, after Role scope declaration) — "Before briefing ANY subagent, the main agent must read … requirements.md … design.md … tasks.md"
- **Location 2:** "Context reset discipline for orchestrator" (under Rule 2) — "Before dispatching each wave: Re-read the spec (requirements.md + design.md) fresh"
- **Location 3:** "Rule 7 — Session Start Protocol / Session start reading order" — steps 4–6 repeat the same read of requirements.md, design.md, tasks.md
- **Assessment:** All three say the same thing with slightly different framing. The Session Start Protocol (Rule 7) is the canonical place; the other two are weaker duplicates.
- **Recommended action:** Keep Rule 7 as canonical. In "Mandatory spec reads at session start" and "Context reset discipline," replace content with a cross-reference: "See Rule 7 — Session Start Protocol."

### D-02: Build retry cap stated twice
- **Location 1:** "Build retry cap" section (under Rule 2) — "The retry cap is 3 attempts … On the third failure: Set task-log status to `Build failure`"
- **Location 2:** "Kill criteria for stuck subagents" table — "3 build failures with no diagnostic improvement → Kill"
- **Location 3:** "Subagent exit checklist" step 2 — "If build fails, apply the build retry cap (max 3 attempts)"
- **Assessment:** The rule is the same (3 attempts), but defined in one place and referenced in two others. Location 2 and 3 are references, not definitions, so this is mild. However, Location 2's kill criterion ("3 failures with no diagnostic improvement") adds a qualifier ("no diagnostic improvement") absent from the Build retry cap definition, creating a subtle inconsistency.
- **Recommended action:** Keep "Build retry cap" as the definition. Remove the qualifier "with no diagnostic improvement" from Kill criteria (or add it to Build retry cap). Add an explicit cross-reference from exit checklist to the definition.

### D-03: "Spec gap → blocked: spec gap → stop" stated twice
- **Location 1:** "Spec gap escalation — documentation requirement" — "If `Blocking: Yes`, set task-log status to `blocked: spec gap` and stop."
- **Location 2:** "Pre-task context gate" — "Spec files missing → set task status to `blocked: spec gap`, stop"
- **Assessment:** Both describe the same action. The first is broader (any spec gap); the second is specific (missing spec files). The general rule subsumes the specific.
- **Recommended action:** Keep "Spec gap escalation" as the canonical rule. In "Pre-task context gate," reference it: "set status per Spec Gap Escalation protocol."

### D-04: "Subagents must not write specs" stated twice
- **Location 1:** "Spec ownership constraint" — detailed table of what subagents may/may not do
- **Location 2:** "Subagent scope constraint — no unilateral redesign" — "subagents must NOT … Rename entities, DTOs, or methods to names that differ from the spec"
- **Location 3:** Rule 2a Approval Authority Matrix — "Spec content (requirements.md, design.md) — Approver: Helder — Subagents may not write or rewrite specs"
- **Assessment:** The prohibition is stated in all three places. Spec ownership is the canonical section; the other two partially overlap.
- **Recommended action:** Keep "Spec ownership constraint" as the canonical definition. In Rule 2a table, keep the row but add "see Spec ownership constraint." In "Subagent scope constraint," remove the spec-writing prohibition and add a cross-reference.

### D-05: "findings.md" defined in two places
- **Location 1:** "Spike validation task pattern" — "Artifact: `Docs/specs/[feature]/findings.md`"
- **Location 2:** "Discovery mode" — "create `Docs/specs/[feature]/findings.md` documenting: What was tried …"
- **Location 3:** "findings.md — session artifact for exploratory work" — full format spec including the `findings.md format` code block
- **Assessment:** The spike pattern and discovery mode both reference the findings artifact, and then a third section defines the full format. The format definition in the third section is canonical, but the other two also describe what findings.md should contain (partially overlapping).
- **Recommended action:** In "Spike validation task pattern" and "Discovery mode," remove the inline content lists and cross-reference the canonical "findings.md — session artifact" section.

### D-06: "Spec size calibration" table and "Spec ceremony calibration table" overlap heavily
- **Location 1:** "Spec size calibration" table (under Rule 1) — maps task sizes (Tiny/Small/Medium/Large/Epic) with estimated effort to spec size target
- **Location 2:** "Spec ceremony calibration table" (also under Rule 1) — maps task types with estimated effort to ceremony level and required artifacts
- **Assessment:** Both tables cover the same decision: how much spec work to do given task size. They use different category systems (size vs task type) and different terminology, but answer the same question. They partially align (e.g., "Cross-layer feature (2–8 hours)" = "Medium" in size table), but someone reading one table gets a different framing than someone reading the other.
- **Recommended action:** Merge into one table. Keep the "Spec ceremony calibration table" structure (it is more detailed) and add a "Spec size target" column. Remove the separate "Spec size calibration" table.

### D-07: "When to skip SDD" table and "Spec ceremony calibration table" overlap
- **Location 1:** "When to skip SDD (spec bypass rule)" table — maps task types to "Spec required? Yes/No" and minimum artifact
- **Location 2:** "Spec ceremony calibration table" — includes task types with "Ceremony level: None" (which implies no spec)
- **Assessment:** A reader needs to consult two tables to determine: (a) should I write a spec at all, and (b) if so, how much. These should be one decision flow, not two tables.
- **Recommended action:** Merge "When to skip SDD" into the calibration table by adding a "Spec required?" column, or add a "None — no spec" row at the top of the calibration table that maps to the bypass cases.

### D-08: "Session resume rule" stated twice
- **Location 1:** "Multi-session state handoff protocol" — "Session resume rule: At the start of a new session, read the handoff artifact first — before reading MASTER_PLAN.md or the spec."
- **Location 2:** "Rule 7 — Session Start Protocol / Session start reading order" — step 2: "Active session handoff file … overrides MASTER_PLAN for exact continuation point"
- **Assessment:** Both say the same thing: read the handoff file first. Rule 7 is canonical.
- **Recommended action:** In "Multi-session state handoff protocol," remove the "Session resume rule" sentence and add: "See Rule 7 — Session Start Protocol for the full reading order."

### D-09: "No commit / push" in subagent exit checklist vs. return protocol
- **Location 1:** "Subagent return protocol — status signal only" — "Committing and pushing all changes (`git push origin HEAD`)"
- **Location 2:** "Subagent exit checklist" steps 7–8 — "Commit … Push `git push origin HEAD`"
- **Assessment:** Both enumerate the same commit+push requirement. Minor duplication.
- **Recommended action:** In "Subagent return protocol," replace steps 2–3 with "Follow Subagent exit checklist steps 7–8."

### D-10: "Post-wave verification" stated in multiple places
- **Location 1:** "Post-wave verification — main agent runs build independently" — full protocol with steps 1–4
- **Location 2:** "When to take back control" — "After the subagent returns: run `dotnet build` and `dotnet test` as main agent"
- **Location 3:** "Rule 2b / Review SLA enforcement" — "Elevated tasks require the main agent to run `dotnet build` + `dotnet test` + E2E check"
- **Assessment:** The core requirement (main agent runs build after each wave) is stated in three places. Location 1 is the canonical section.
- **Recommended action:** "When to take back control" should be merged into or eliminated in favor of the canonical "Post-wave verification" section. Rule 2b can reference the section.

### D-11: "Demo statement" requirement stated twice
- **Location 1:** "Demo statement requirement" (under Rule 1) — defines the format and purpose
- **Location 2:** "Task atomization checklist" (under Rule 4) — "The task has a `Demo:` statement or a clear acceptance criterion it satisfies"
- **Location 3:** "Task entry format — structured fields" — "Demo: [one sentence …]" as a required field
- **Assessment:** The demo statement is defined once but its requirement is restated in multiple places, each adding slightly different framing. Not a problem per se, but Location 1's definition should be the canonical reference point.
- **Recommended action:** Acceptable as-is, since Rule 1 defines it and Rule 4 references it. No merge needed, but consider adding explicit cross-references to the definition.

### D-12: "Regeneration test" and "Rebuild test" are the same concept named differently
- **Location 1:** "Regeneration test practice" (under Rule 1) — "Give the spec … to a fresh Claude session and ask it to implement the feature. If the output contradicts your intended design in more than 2 places, the spec has gaps."
- **Location 2:** "Rebuild test — feature close-out spec quality check" (also under Rule 1) — "In a fresh Claude session … provide only the spec and ask: 'Implement this feature'. Compare the generated output against the actual implementation." Threshold: 4+ divergences = significant gaps.
- **Assessment:** These are the same diagnostic. "Regeneration test" threshold is >2 contradictions; "Rebuild test" uses a 4-level scale (0–1, 2–3, 4+). The second section refines the first but does not reference it, so a reader may not realize they are the same practice.
- **Recommended action:** Merge into one section. Keep the "Rebuild test" name (it is later and more detailed). Remove "Regeneration test practice" and replace with a forward reference to "Rebuild test."

### D-13: "Mandatory spec reads at session start" is structurally placed inside Rule 2 but covers Rule 7 material
- **Location:** Appears under Rule 2 (Subagent Delegation) after the Role scope declaration, separated by a `---` horizontal rule, yet covers session-start reading behavior
- **Assessment:** The section header says "Mandatory spec reads at session start" — this belongs under Rule 7 (Session Start Protocol), not inside Rule 2.
- **Recommended action:** Move content to Rule 7 (or merge with the Session Start reading order). The `---` separator before it suggests it was added as an afterthought and never relocated.

### D-14: "ACTIVE-CONSIDERATIONS.md" described in two places
- **Location 1:** "ACTIVE-CONSIDERATIONS.md — session priority stack" — full format and update protocol
- **Location 2:** "Tiered memory governance" table (under Rule 7) — "Session tier: `ACTIVE-CONSIDERATIONS.md`, `session-handoff.md`"
- **Location 3:** "Session start constraint capture" — references ACTIVE-CONSIDERATIONS.md as a destination for open questions
- **Assessment:** The artifact is introduced in Location 1 (under Rule 2) and referenced in Rule 7. The definition should be in Rule 7 since it is a session management artifact, not a subagent delegation artifact.
- **Recommended action:** Move "ACTIVE-CONSIDERATIONS.md — session priority stack" from Rule 2 to Rule 7. In Rule 2, add a brief reference: "See ACTIVE-CONSIDERATIONS.md — session priority stack (Rule 7)."

---

## Contradictions

### C-01: "All coding is done by subagents" vs. "dotnet ef migrations add" in main agent column
- **Rule A:** Rule 2 opening statement — "All coding is done by subagents. The main agent handles shell-only steps."
- **Rule B:** Rule 2 table — Main agent column lists `dotnet ef migrations add`
- **Assessment:** EF migrations are generated code (a coding task), yet the table assigns `dotnet ef migrations add` to the main agent. This contradicts the stated separation. The intent is probably that the main agent runs the CLI command while a subagent writes the subsequent migration configuration code — but the table doesn't clarify this.
- **Recommended action:** Add a footnote to the table: "`dotnet ef migrations add` generates the scaffold; the subagent then edits the migration file as needed." Or move it to the Subagent column with a note that the CLI command must be run in the main agent's shell.

### C-02: "Spec bypass rule" allows no spec for "small isolated change (< 1 hour, single file)" but "Spec ceremony calibration table" requires `tasks.md` entry for same
- **Rule A:** "When to skip SDD" table — "Small isolated change (< 1 hour, single file, no interface change) → Spec required: No → Minimum artifact: Descriptive commit message"
- **Rule B:** "Spec ceremony calibration table" — "Small isolated change (1 file, no interface change, < 1 hour) → Ceremony level: Light → Required artifacts: `tasks.md` entry + commit message"
- **Assessment:** For the same task type (small isolated, <1 hour, single file), one table says no spec and a commit message is sufficient; the other says a `tasks.md` entry is also required. These contradict.
- **Recommended action:** Reconcile to one answer. The bypass rule (no tasks.md) is simpler and consistent with the "commit message as spec" philosophy. Remove the `tasks.md` requirement from the calibration table for this tier, or add a note that `tasks.md` applies only when the task is part of an active feature plan.

### C-03: Subagent may add spec update note vs. subagents may not write specs
- **Rule A:** "Spec ownership constraint" exception — "A subagent may add a `> **Spec updated [date]:** one-line note` to an existing spec file"
- **Rule B:** "Living spec protocol — write decisions back before stopping" — "For each such decision, add a `> **Spec updated [YYYY-MM-DD]:** [decision summary]` note to the relevant spec file … If the decision is a Key Decision (architecture-level), add it to the `Key Decisions` section of `design.md`"
- **Rule C:** Rule 2a Approval Authority Matrix — "Spec content (requirements.md, design.md) — Approver: Helder — Subagents may not write or rewrite specs"
- **Assessment:** Rule A and B permit subagents to update spec files (with constraints). Rule C says Helder is the approver for all spec content. These are in tension: the Living spec protocol actively instructs subagents to write back to design.md Key Decisions, which is spec content. Rule A's "only to reflect a decision that was explicitly authorized by the main agent" qualifier partially reconciles it, but Rule B has no such qualifier.
- **Recommended action:** Add the authorization qualifier to the Living spec protocol: "write back only decisions that were within the task's authorized scope." Align Rule 2a to explicitly permit the one-line note pattern as an exception.

### C-04: "Wave handoff" says inject actual contracts verbatim vs. "Briefing protocol" says paths only, never paste content
- **Rule A:** "Wave handoff — inject actual contracts for new artifacts" — "Include these extracted signatures verbatim in the next wave's briefing … Contracts from previous wave … [verbatim interface definition]"
- **Rule B:** "Briefing protocol — paths only, never paste content" — "Subagent briefings must reference file paths, not paste file content inline."
- **Assessment:** The wave handoff rule explicitly tells the main agent to paste verbatim interface signatures into briefings. The briefing protocol says never paste content. These directly contradict.
- **Recommended action:** Add an explicit exception to the Briefing protocol: "Exception: interface/DTO signatures produced in the previous wave may be included verbatim as 'Contracts from previous wave' — these are committed code, not rule file content, and their size is bounded." Reference the Wave handoff section.

### C-05: "Multi-wave checkpoint every second wave" vs. "After every wave: spec rot check"
- **Rule A:** "Multi-wave checkpoint pattern" — "the main agent must perform a multi-wave checkpoint after every second wave"
- **Rule B:** "Spec rot — detection and prevention / Prevention protocol" step 1 — "After every wave: run the spec rot check (re-read spec + compare against committed code)"
- **Assessment:** Rule A says check every 2 waves. Rule B says check after every wave. These conflict on frequency.
- **Recommended action:** Clarify that the "spec rot check" after every wave is a lighter check (do the indicators look normal?), while the "multi-wave checkpoint" every second wave is a deeper structured protocol. Explicitly state this distinction. Currently a reader following both rules would either check twice as often as needed or be confused about which takes precedence.

---

## Inconsistencies

### I-01: Rule numbering is non-sequential (Rules appear as: 1, 2, 2a, 2b, 3, 3a, 4, 5, 6, 7, 8)
- **Occurrence 1:** Rules are numbered 1, 2, 3, 4, 5 at the top-level, then 2a, 2b, 3a were added later
- **Occurrence 2:** Rules 6, 7, 8 appear after Rule 5, out of expected order (Rule 7 appears after Rule 8's jump)
- **Assessment:** The heading sequence in the file is: Rule 1, Rule 2, Rule 3, Rule 2a, Rule 2b, Rule 3a, Rule 4, Rule 8, Rule 7, Rule 6, Rule 5. This is not navigable without reading the whole file. Rules were added incrementally and not renumbered.
- **Recommended action:** Renumber all rules sequentially: 1 (Spec-First), 2 (Subagent Delegation), 3 (Commit After Every Task), 4 (Tasks.md), 5 (Task Status), 6 (Research Tool Gate), 7 (Session Start), 8 (GitHub MCP). Absorb the "a/b" sub-rules into their parent rules.

### I-02: "3 attempts" vs. "3 strikes" — same thing, different framing
- **Occurrence 1:** "Build retry cap" — "retry cap is 3 attempts"
- **Occurrence 2:** "Kill criteria / 3-strike error recovery protocol (OPP-8-14)" — "First strike … Second strike … Third strike"
- **Assessment:** Both describe 3 tries before stopping, but "attempts" vs "strikes" creates cognitive load. They are not the same gate: the build retry cap applies to a single subagent's build errors; the 3-strike recovery applies to repeated subagent dispatch attempts. The terminology overlap is confusing.
- **Recommended action:** Make the distinction explicit: rename one to avoid the confusion. For example, "build retry cap" stays; "3-strike protocol" becomes "3-dispatch escalation protocol" to clarify it applies to re-dispatching, not individual build retries.

### I-03: "Spec updated [date]" note format inconsistently quoted
- **Occurrence 1:** "Spec versioning discipline" — `> **Spec updated [YYYY-MM-DD]:** [one sentence]`
- **Occurrence 2:** "Spec ownership constraint" exception — `> **Spec updated [date]:** one-line note`
- **Occurrence 3:** "Living spec protocol" — `> **Spec updated [YYYY-MM-DD]:** [decision summary]`
- **Assessment:** Location 2 uses `[date]` while 1 and 3 use `[YYYY-MM-DD]`. Minor inconsistency in the placeholder text; the format is the same, but agents may copy either version and produce inconsistently formatted notes.
- **Recommended action:** Standardize to `[YYYY-MM-DD]` everywhere.

### I-04: "Spec size calibration" uses different task-size vocabulary than "Spec ceremony calibration table"
- **Occurrence 1:** "Spec size calibration" categories: Tiny, Small, Medium, Large, Epic
- **Occurrence 2:** "Spec ceremony calibration table" categories: "Typo fix," "Single-file cosmetic change," "Small isolated change," "Multi-file change within one layer," "Cross-layer feature," "Multi-session feature," "Architectural change"
- **Assessment:** Two different vocabularies for the same concept (how big is this task). Neither table cross-references the other. This forces a reader to mentally translate between the two systems.
- **Recommended action:** Merge as recommended in D-06. If kept separate, add a mapping note: "Tiny ≈ Typo fix/cosmetic, Small ≈ Small isolated change," etc.

### I-05: "Two-tier spec trigger" threshold inconsistency
- **Occurrence 1:** "Spec size calibration" section — "Two-tier spec trigger: Any task estimated at > 2 hours OR touching ≥ 2 layers automatically requires a full three-file spec."
- **Occurrence 2:** "SDD decision table for medium-complexity tasks" — "Change touches ≥ 2 layers → Write `design.md` before starting" (not "full three-file spec")
- **Assessment:** The two-tier spec trigger says ≥ 2 layers → full three-file spec. The SDD decision table says ≥ 2 layers → just write design.md. These differ in what is required.
- **Recommended action:** Reconcile. If the intent is that ≥ 2 layers always requires all three files, update the decision table row to say "Full three-file spec." If design.md alone is sometimes sufficient for ≥ 2 layers, update the trigger statement.

### I-06: "Verifier subagent" described as optional but also as mandatory in some cases
- **Occurrence 1:** "Verifier subagent" — "a Verifier subagent may be dispatched to independently validate"
- **Occurrence 2:** Rule 2b — "Architectural tasks require … Verifier subagent" as part of Architectural review lane
- **Assessment:** "May be dispatched" (optional) vs. "require … Verifier subagent" (mandatory for Architectural lane). These conflict on when the Verifier is required.
- **Recommended action:** Restate the Verifier section as "optional unless the task is in the Architectural review lane, in which case it is mandatory."

---

## Structural Issues

### S-01: Rules appear out of numerical order in the file
- **Issue:** The top-level sections appear in this order: Rule 1, Rule 2, Rule 3, Rule 2a, Rule 2b, Rule 3a, Rule 4, Rule 8, Rule 7, Rule 6, Rule 5. This is the order rules were added over time, not a logical reading order. A reader looking for Rule 5 must skip Rule 8, 7, and 6 to find it.
- **Recommended action:** Reorder sections to match rule numbers: 1, 2 (with 2a/2b absorbed), 3 (with 3a absorbed), 4, 5, 6, 7, 8.

### S-02: "Mandatory spec reads at session start" is stranded inside Rule 2
- **Issue:** The section appears after the Role scope declaration in Rule 2, separated by a horizontal rule that makes it look like a separate top-level section. Its content (session start behavior) belongs in Rule 7 — Session Start Protocol.
- **Recommended action:** Move this section to Rule 7 and merge with "Session start reading order."

### S-03: Session management sections are scattered across Rule 2 and Rule 7
- **Issue:** The following session management sections all appear under Rule 2 (Subagent Delegation), even though they govern the main agent / orchestrator behavior at session boundaries, not subagent behavior:
  - "ACTIVE-CONSIDERATIONS.md — session priority stack"
  - "Multi-session state handoff protocol"
  - "Context exhaustion warning signs"
  - "Context reset discipline for orchestrator"
  - "Wave completion discovery briefs"
  These belong in Rule 7 (Session Start Protocol) or in a dedicated "Session Management" rule.
- **Recommended action:** Move all five sections to Rule 7 or create a new Rule "Session Management" to house them. This would reduce Rule 2's length by ~30% and make it focussed on subagent delegation mechanics.

### S-04: "When to skip SDD" placed after multiple detailed spec sections
- **Issue:** "When to skip SDD (spec bypass rule)" appears near the end of the Rule 1 material, after many detailed sections (tacit knowledge, portability, size calibration, failure-mode analysis, etc.). Logically, a bypass rule should appear early — before the reader invests in reading detailed spec guidance that may not apply to their task.
- **Recommended action:** Move "When to skip SDD" to immediately after "SDD Invariant" or at the beginning of Rule 1, so readers know early whether they need the full ceremony.

### S-05: "Spec ceremony calibration table" and "When to skip SDD" are separated by several sections
- **Issue:** These two tables address the same decision (how much spec work to do) but appear in different locations under Rule 1. A reader must scroll back and forth to compare them.
- **Recommended action:** Place them adjacent, or merge them (see D-06, D-07).

### S-06: "Rule 2a" and "Rule 2b" appear after Rule 3, breaking reading flow
- **Issue:** Rule 2a (Approval Authority Matrix) and Rule 2b (Review SLA) appear after Rule 3 in the document. They logically extend Rule 2 but a reader who reads sequentially encounters them only after finishing Rule 3.
- **Recommended action:** Move Rule 2a and 2b to immediately follow Rule 2. Alternatively, absorb their content into Rule 2 as subsections and remove the separate rule headers.

### S-07: "Hook health verification" under "Hook Enforcement Notes" vs. session start behavior
- **Issue:** "Hook health verification" instructs the agent to check `.claude/settings.json` at the start of each session. This is a session-start behavior, but it appears in the "Hook Enforcement Notes" section at the top — not in Rule 7 (Session Start Protocol) where all session-start behaviors are catalogued.
- **Recommended action:** Move "Hook health verification" to Rule 7's session start reading order as step 0, or add a cross-reference in Rule 7: "Before reading anything else, verify hook health (see Hook Enforcement Notes)."

### S-08: "Subagent exit checklist" is placed in the middle of Rule 2 subsections rather than at the end
- **Issue:** The exit checklist (8 steps, marked "mandatory before returning") is the final action a subagent takes, but it appears in the middle of a long sequence of Rule 2 subsections (before Kill criteria, Build retry cap, Intent verification, E2E gate, etc.). A reader implementing a subagent must scan past several other sections to find the checklist — the most action-critical section for any subagent.
- **Recommended action:** Move "Subagent exit checklist" to the last subsection of Rule 2, immediately before the Rule 3 separator. It is the summary of everything a subagent must do before stopping — it should read last.

---

## Priority Summary

| ID | Type | Severity | Recommended action |
|----|------|----------|--------------------|
| C-04 | Contradiction | High | Reconcile "paste verbatim" vs "paths only" rule in briefing protocol |
| C-02 | Contradiction | High | Reconcile tasks.md requirement for small isolated changes |
| C-05 | Contradiction | High | Clarify spec rot check frequency (every wave vs every second wave) |
| S-01 | Structural | High | Reorder rules to numerical sequence |
| S-03 | Structural | High | Move session management sections from Rule 2 to Rule 7 |
| D-12 | Duplication | High | Merge "Regeneration test" and "Rebuild test" into one section |
| D-06 | Duplication | High | Merge "Spec size calibration" and "Spec ceremony calibration table" |
| C-01 | Contradiction | Medium | Clarify EF migration CLI ownership in Rule 2 table |
| C-03 | Contradiction | Medium | Align subagent spec-write permissions across three sections |
| I-01 | Inconsistency | Medium | Renumber rules sequentially |
| I-05 | Inconsistency | Medium | Reconcile "two-tier spec trigger" threshold with SDD decision table |
| I-06 | Inconsistency | Medium | Clarify when Verifier is optional vs mandatory |
| D-01 | Duplication | Medium | Consolidate spec re-read reminders under Rule 7 |
| D-07 | Duplication | Medium | Merge "When to skip SDD" with ceremony calibration table |
| S-02 | Structural | Medium | Move "Mandatory spec reads at session start" to Rule 7 |
| S-04 | Structural | Medium | Move "When to skip SDD" earlier in Rule 1 |
| S-06 | Structural | Medium | Move Rule 2a/2b to follow Rule 2 |
| S-08 | Structural | Medium | Move "Subagent exit checklist" to end of Rule 2 |
| D-02 | Duplication | Low | Reconcile build retry cap qualifier in Kill criteria |
| D-03 | Duplication | Low | Cross-reference spec gap escalation from pre-task context gate |
| D-04 | Duplication | Low | Add cross-references for subagent spec-write prohibition |
| D-05 | Duplication | Low | Cross-reference findings.md format from spike/discovery sections |
| D-08 | Duplication | Low | Remove "Session resume rule" from handoff protocol; reference Rule 7 |
| D-09 | Duplication | Low | Cross-reference exit checklist from return protocol |
| D-10 | Duplication | Low | Remove "When to take back control" section (absorbed by Post-wave verification) |
| D-11 | Duplication | Low | Add cross-references to demo statement definition |
| D-13 | Duplication | Low | Move "Mandatory spec reads at session start" (structural fix) |
| D-14 | Duplication | Low | Move ACTIVE-CONSIDERATIONS.md definition to Rule 7 |
| I-02 | Inconsistency | Low | Rename "3-strike protocol" to avoid confusion with build retry |
| I-03 | Inconsistency | Low | Standardize spec note date placeholder to [YYYY-MM-DD] |
| I-04 | Inconsistency | Low | Consolidate task-size vocabulary across calibration tables |
| S-05 | Structural | Low | Place spec calibration tables adjacent |
| S-07 | Structural | Low | Move hook health verification to Rule 7 session start |
