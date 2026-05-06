# Exception Registry — MyVocaList

Approved exceptions to constitutional constraints. Review quarterly.

Exceptions that accumulate (3+ for the same constraint) signal the constraint may need amendment.
If a constitutional constraint cannot be followed in a specific case, document it here before deviating.
**Never deviate silently** — silent deviations accumulate as hidden technical debt.

---

## Registry

| Date | Constraint violated | Reason for exception | Code location | Expires |
|------|---------------------|----------------------|---------------|---------|
| —    | —                   | —                    | —             | —       |

---

## How to add an entry

When a constitutional constraint (CLAUDE.md Non-Negotiables, architecture rules, code-principles.md) cannot
be followed in a specific case:

1. Add a row to the table above before writing the non-conforming code.
2. **Date:** ISO date `YYYY-MM-DD`
3. **Constraint violated:** quote the rule (or the file + section)
4. **Reason:** one sentence minimum — what makes this case different
5. **Code location:** file path + line or class name
6. **Expires:** a condition or date after which the exception should be removed, or `permanent` if indefinite

## Quarterly audit

At each quarterly constitutional audit (see CLAUDE.md), review this registry for:
- Exceptions older than their expiry condition — remove the deviation or promote to rule amendment
- Entries where the same constraint is listed 3+ times — the constraint may be wrong; escalate for review
- Entries with no expiry — evaluate whether the exception should become a formal rule amendment
