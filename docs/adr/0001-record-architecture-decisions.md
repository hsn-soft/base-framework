# ADR-0001: Architecture decisions are recorded as ADRs in `docs/adr`

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

The framework was rewritten over a few weeks in many conversations between the maintainer and an AI assistant, and every session starts with an empty
memory. Decisions (why a package exists, where code lives, what a hook does, what a log field means) were made in chat, partly written in the reference
documents (`docs/Logging.md`, `docs/EventBus.md`, ...) as *what is*, but not *why*, and not *what was rejected*. A new session, or a new maintainer, had to rediscover them
from the code, and could undo one without knowing it was deliberate.

## Decision

- Every architecture decision is a short **Architecture Decision Record** in `docs/adr/`, one file per decision: `NNNN-short-title.md`, numbered in the order they are
  taken, written from [the template](0000-template.md). The index is [docs/adr/README.md](README.md).
- An ADR says the **context** (why), the **decision** (what we do), the **consequences** (what we accept and what must be kept in step). It does not describe how to
  use a feature step by step: that is a reference document in `docs/` (`Logging.md`, `LogLevels.md`, ...), which an ADR links to.
- **An ADR is not edited to change a decision.** A new ADR replaces it: the old one gets `Status: Superseded by ADR-NNNN`, the new one says what it replaces and
  why. Small corrections (a typo, a renamed file) are fine; a change of meaning is a new ADR.
- **What deserves an ADR:** a rule other code must follow (dependencies, where code lives, naming, levels, formats), a choice between real alternatives, anything that costs
  effort to reverse or that surprised us. Not: a bug fix, a refactor that keeps the rules, a new feature that follows the existing rules.
- **Every new decision is added when it is taken**, in the same pull request as the change, and the index is updated. The assistant is expected to write the ADR
  as part of the work and to read the index before it proposes something that touches an existing rule.
- The documents are written in English, like the rest of the repository.

## Consequences

- A new session starts from [docs/adr/README.md](README.md) (the root `CLAUDE.md` says so) and finds the rules and their reasons in minutes.
- Writing an ADR is a small cost on every decision; it is accepted because the alternative is deciding the same thing again.
- The reference documents may repeat a rule for the reader's convenience; the ADR is the authority and the reference document links to it.
- Decisions taken before this ADR were written down afterwards (ADR-0002 to ADR-0083); their date is the day the decision was taken as best we know it (October 2026), and the ones whose history matters say how they changed.

## Related

[docs/adr/README.md](README.md), [0000-template.md](0000-template.md), `CLAUDE.md` at the root of the repository.
