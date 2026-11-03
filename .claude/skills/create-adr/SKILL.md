---
name: create-adr
description: "Use when a change introduces a lasting architecture rule, package/dependency boundary, workflow decision, or consequential choice that should be recorded as an ADR."
---

# Create or supersede an ADR

## Procedure

1. Read `.claude/rules/documentation-adr.md`, `docs/adr/README.md`, `docs/adr/INDEX.md`, and the closest existing ADRs. Check whether the decision is already made or later superseded.
2. Confirm that this is a lasting rule, consequential alternative, or decision that would be costly or surprising to reverse. Do not create an ADR for a bug fix, routine refactor, or feature that follows existing rules.
3. Select the next unused number by inspecting `docs/adr/` and the full catalog (a decision about a sample is written in `samples/docs/adr/` instead, with that folder's own numbering and its README as the index). Copy `docs/adr/0000-template.md` to `docs/adr/NNNN-short-title.md`; use a concise decision-shaped title, current status, and date the decision is actually taken.
4. Write factual context and rejected alternatives as needed; state the decision as actionable present-tense rules; document trade-offs and what must stay in sync (code, tests, docs, CI, scripts). Link related ADRs and feature references.
5. Never edit an old ADR to silently change its decision. For a changed decision, mark the old ADR superseded, create a new ADR that names what it supersedes and why, and describe migration/consequences.
6. Add the new ADR row in the appropriate section of `docs/adr/INDEX.md` and update `docs/adr/README.md` only if its task-to-ADR guidance needs a change. Update the canonical operational/reference document if users need the resulting behavior.
7. Review all links, numbering, statuses, and supersession notes. Keep the ADR and implementation in the same change.
