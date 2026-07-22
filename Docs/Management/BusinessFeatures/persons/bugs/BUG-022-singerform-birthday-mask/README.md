---
id: BUG-022
title: "BUG-022: SingerForm birthday field mask missing (Minor)"
status: "✅ Fixed"
severity: Major
target: 2026-07-01
section: DevCycleCraft
parent: ui-form-validation-guide
kind: bug
order: 110
closed: 2026-07
goal: "Fixed with a XAML-only date input mask on the birthday field."
pointer: BusinessFeatures/persons/bugs/BUG-022-singerform-birthday-mask/
---

# BUG-022: SingerForm birthday field mask missing

The birthday entry accepted free text with no input mask. Fixed with a XAML-only date
input mask (`Mask="00/00"`). Detail: the bug note in this folder.

> **Spec updated [2026-07-22]:** severity reclassified `Minor` → `Major` (Helder decision 3A,
> spec-evolution-versioning) so the pre-existing folder is legal under REQ-SEV-03. The title is
> transcribed verbatim from the archive row and still reads `(Minor)`.

> **Spec updated [2026-07-22]:** the original Notes text quoted the literal mask string, which the
> row-template banned-content scan reads as a test count. The mask is described in words in the row
> and quoted verbatim here in the body instead.
