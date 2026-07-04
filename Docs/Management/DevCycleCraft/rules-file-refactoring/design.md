# Rules File Refactoring — Design

## Approach Overview

The refactoring converts `.claude/rules/*.md` files from **comprehensive all-in-one reference documents** to **minimal 1–2 page routing tables**, with detailed patterns moved to `.claude/library/` files and referenced via enabled superpowers skills.

### Current architecture (before)
```
.claude/rules/
├── workflow.md           (11.5k tokens — SDD workflow, 8 rules, examples)
├── testing.md            (8.3k tokens — TDD procedures, test patterns, anti-patterns)
├── code-principles.md    (3k tokens — architecture, naming, DI, EF Core)
├── constraints-registry.md (2.3k tokens — discovered limits, DevExpress/EF/MAUI)
├── bug-tracking.md       (1.2k tokens — BUG-NNN scheme, severity, regression tests)
├── component-change-governance.md (1k tokens — 4-gate process for shared components)
└── mediatr-patterns.md   (1.1k tokens — command/query/event patterns reference)
├── Total: 28.4k tokens (always loaded, even if agent doesn't use specific rule)

Superpowers skills (disabled to save tokens):
├── brainstorming         (≈3k tokens, loads the full thing even if user runs 1 command)
├── writing-plans         (≈3k tokens)
├── test-driven-development (≈3k tokens)
└── code-review           (≈2k tokens)
└── Total: ≈11k tokens when enabled, but only ~100 tok for descriptions at session start

BACKLOG entry shows rules actually ~33.3k due to aggregation in memory system.
```

### Proposed architecture (after)
```
.claude/rules/
├── workflow.md           (1 page: Rules 1–8 routing table → linked to .claude/library files + superpowers)
├── testing.md            (1 page: Routing table → linked test-driven-development skill + library)
├── code-principles.md    (1 page: Routing table → linked dotnet-skills + architecture constraints)
├── constraints-registry.md (1 page: Routing table → indexed constraint categories in library)
├── bug-tracking.md       (0.5 pages: Routing table → severity/regression tables in library)
├── component-change-governance.md (0.5 pages: Routing table → governance gates in library)
├── mediatr-patterns.md   (stub or removed entirely)
└── Total: ~2–3k tokens (minimal routing tables)

.claude/library/
├── spec-writing-guide.md (NEW: decision table, AC format, examples — extracted from workflow.md Rules 1 + existing spec-guide)
├── subagent-patterns.md (NEW: task sizing, wave parallelism, checklist — extracted from workflow.md Rule 2)
├── research-tool-selection.md (NEW: Context7 → Exa → WebSearch hierarchy — extracted from workflow.md Rule 6)
├── component-safety-gate.md (NEW: 4-gate process checklist — extracted from component-change-governance.md)
├── code-style-reference.md (NEW: architecture/naming/style reference — extracted from code-principles.md)
├── testing-reference.md (NEW: test patterns, anti-patterns, structure — extracted from testing.md)
└── ... (other existing library files)

Superpowers skills (re-enabled):
├── brainstorming         (loads description ~50 tok, full body on first use)
├── writing-plans         (same model)
├── test-driven-development (same model)
└── code-review           (same model)
└── Total per-session: ~200 tok initial (descriptions only), 14k on-demand (lazy-loaded skill bodies)

Net result: Sessions start lighter (2–3k rules + 200 tok skills ≈ 2.3k total), with 14k on-demand per unique skill invoked.
```

## Refactoring Strategy

### Incremental phases (12 tasks)

**Phase 0: Spike (BLOCKING)**
- Pilot pattern: refactor `code-principles.md` (smallest, pure reference)
- Extract 2 sections → new library files
- Enable 1 skill (`dotnet-skills`)
- Verify no content loss, skill fires, no workflow change
- Artifact: `pilot-findings.md`

**Phase 1–5: Small files** (tasks 01–05)
- Refactor 5 smaller rules files (code-principles, mediatr-patterns, bug-tracking, component-change-governance, constraints-registry)
- ~1–2k tokens saved each
- Sequential (one per agent wave)

**Phase 2–3: Large files** (tasks 06–10)
- Refactor workflow.md (3 waves, breaking into Rules 1–2, 3–5, 6–8)
- Refactor testing.md (2 waves, breaking into TDD phases and test types)
- ~4k tokens saved per wave

**Phase 4: Skill re-enablement** (tasks 11–12)
- Re-enable `brainstorming`, `writing-plans`, `test-driven-development`, `code-review` in settings.json
- Verify on-demand loading works
- Update CLAUDE.md § Skill & MCP Lookup table
- Final measurement: `/context fresh` showing token recovery

### Verification gates (per task)

1. **Routing table check** — refactored rules file fits on 1–2 pages; every line either removed, moved to library, or has skill pointer
2. **Skill invocation test** — for every library/skill pointer in routing table, invoke the skill and confirm it works
3. **Content integrity** — re-read the original rules file + new library file; confirm zero net content loss
4. **Agent workflow test** — run a fresh mini-workflow (brainstorm → plan snippet) using refactored rules + enabled skill; confirm agents can find guidance without loading the entire old rules file

## Key Design Decisions

### Why extract to .claude/library/ and not superpowers skills directly?

**Decision:** Create library files (.claude/library/*.md) AND point to superpowers skills, not replacing skills with library files.

**Rationale:**
1. **Superpowers skills are read-only** — agents invoke them but cannot edit them. Some customization (MyVocaList-specific examples, DRY Onion task ordering) belongs in library files, not upstream in the generic skill.
2. **Layered guidance** — `.claude/library/` files can reference both generic superpowers skill content AND project-specific context (DRY Onion rule for MyVocaList, specific to this architecture).
3. **Future-proofing** — if Anthropic updates the brainstorming skill, the .claude/library/spec-writing-guide.md can still enforce MyVocaList conventions on top of it.

### Why not just delete the rules files entirely?

**Decision:** Keep routing tables in .claude/rules/*.md as thin 1-page stubs/tables.

**Rationale:**
1. **Agent familiarity** — agents already search for CLAUDE.md → rules files. Keeping the files there with routing tables is lower friction than forcing agents to discover new library files.
2. **Git history continuity** — rules files have 2+ years of amendments; keeping them allows `git blame` and `git log` to continue tracing decisions.
3. **Local-first offline access** — a `.claude/rules/workflow.md` routing table is readable offline; a skill requires a network call to the MCP.

### Routing table format

```markdown
# Workflow Rules — Routing Table

> For detailed guidance, invoke the relevant skill or library file.

## Rule 1 — Spec-First
See `.claude/library/spec-writing-guide.md` for requirements.md/design.md anatomy, AC format, rebuild test, and decision table.
Skill reference: `superpowers:writing-plans` (for plan writing post-spec).
Link to workflow protocol: `spec-quality-gate-review` in library guide.

## Rule 2 — Subagent Delegation
See `.claude/library/subagent-patterns.md` for task sizing, wave limits, single-writer rules, and pre-dispatch checklist.
Skill reference: `superpowers:subagent-driven-development` (for full delegation workflow).
Link to subagent exit checklist: in library patterns.

[... remaining 6 rules in same format ...]
```

Each routing table entry is **one paragraph** — no duplication, no examples, no procedure details. All details live in the library file or skill.

## Migration Checklist (per refactored rules file)

For each rules file being refactored:

- [ ] **Identify extraction targets** — which sections are pure reference (move to library) vs. procedure (link to skill)?
- [ ] **Create library files** — one new `.claude/library/*.md` per major extraction
- [ ] **Rewrite rules file as 1-page routing table** — every rule becomes a paragraph with section/skill/library links
- [ ] **Run skill invocation test** — for every skill link in routing table, invoke the skill and confirm it loads
- [ ] **Verify content integrity** — original file + new library ≠ content loss; re-read both
- [ ] **Update CLAUDE.md pointers** — if rules file is referenced in CLAUDE.md, update pointer to routing table + library file
- [ ] **Commit with rationale** — commit message notes token savings and any content moved
- [ ] **Task-log update** — record Changed files, verification evidence, content integrity affidavit

## Success Criteria (Design perspective)

1. **Minimal routing tables** — each rules file after refactor is ≤2 pages
2. **Zero content loss** — every rule line accounted for (moved, removed with rationale, or kept)
3. **Skill invocation 100% coverage** — every superpowers skill pointer in routing tables is actually used
4. **Agent workflow unchanged** — agents run exact same procedures; only documentation source changes
5. **Token recovery measured** — `/context fresh` shows 14k on-demand tokens recovered per skill invoked
