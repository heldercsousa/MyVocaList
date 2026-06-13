# Wave 0 Spike — Fuzzy Similarity Library

> Time-boxed spike (2026-06-13). Throwaway code reverted; worktree left clean.

## Decision

**Use FuzzySharp 2.0.2** (`FuzzySharp` NuGet, FuzzyWuzzy C# port) for on-device fuzzy entity matching, wrapped behind `ISimilarityScorer`.

## Evidence

| Criterion | Result |
|-----------|--------|
| net10.0-android restore | ✅ succeeded, no FuzzySharp-related warnings (pre-existing NU1608 + DX trial warnings only) |
| net10.0-android build | ✅ 0 errors, 2m17s cold build; no new warnings |
| Native / platform dependency | ❌ none — pure managed (net45…netstandard2.1); no `.so`/`.dylib`/interop. Android + iOS safe |
| Determinism | ✅ pure in-memory, no I/O |

## Scores (`Fuzz.TokenSetRatio`, normalized 0..1; after NFD normalize + lowercase)

| Pair | Score | Verdict |
|------|-------|---------|
| "Björk" → "Biork" | 0.80 | match (accent + typo) |
| "Não Sei" → "Nao Sei" | 1.00 | match (accent only) |
| "Queen" → "Qween" | 0.80 | match (typo) |
| "Bohemian Rhapsody" → "...(Live)" | 1.00 | match (token-set ignores extra tokens) |
| identical | 1.00 | baseline |
| "Queen" → "Madonna" | 0.17 | correctly rejected |

## Recommended threshold

**0.82** (confirms the design's provisional value). All legit variants score ≥ 0.80; nearest unrelated pair is 0.17 — wide margin.

## Critical implementation note — in-memory normalization is required AND compliant

FuzzySharp compares Unicode code points directly. Raw "Björk" vs "Biork" scores only 0.60; after NFD diacritic-strip + lowercase it scores 0.80. Therefore `SimilarityScorer.Score(a,b)` MUST NFD-normalize (`String.Normalize(NormalizationForm.FormD)` → drop `NonSpacingMark` chars → `ToLowerInvariant`) internally before calling FuzzySharp.

**Why this does NOT violate the constraints-registry "no C#-side normalization" hard rule:**
- That rule governs **DB search / uniqueness / duplicate-detection queries** — its rationale is (1) accent-correctness and (2) avoiding full table scans on un-indexed computed values.
- Here the normalization is **in-memory only**, applied to a **bounded candidate pool already retrieved via DB-side `NOCASE_NOACCENT` collation** (no full scan), and is used **only to score advisory candidates the user confirms**. The DB unique index + collation remain the sole authority for the actual insert/update decision (INV-2/INV-3).
- No `*Normalized` column is created; nothing is persisted; the DB layer never sees normalized values.

This is recorded in design.md §5 so the Builder normalizes inside `SimilarityScorer` and a reviewer does not mis-flag it.
