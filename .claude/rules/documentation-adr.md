---
paths:
  - "docs/**/*.md"
  - "README.md"
  - "CONTRIBUTING.md"
---

# Documentation and architecture decisions

- Write documentation and code comments in English. Keep README, contribution guidance, package overview, feature references, and ADRs serving their existing distinct purposes; link to canonical sources instead of duplicating them.
- Feature references explain how behavior works; ADRs explain why a lasting design choice was made. When a rule and a reference disagree, the ADR is authoritative until a new ADR supersedes it.
- Record a new rule other code must follow or a consequential choice between alternatives in a new ADR in the same change. Copy `docs/adr/0000-template.md`, use the next number, and update `docs/adr/INDEX.md`.
- Never edit an ADR to silently change its decision. Add a new ADR that supersedes it, mark the old ADR accordingly, and explain the consequences.
- Before documenting behavior, check the owning implementation and related ADRs. Keep examples and package/dependency claims consistent with `docs/Overview.md`.
- Decisions about a sample are written in `samples/docs/adr/` with their own numbering (not in the catalog here); see `samples/CLAUDE.md`.

See [the ADR guide](../../docs/adr/README.md) and [documentation map](../../docs/README.md). Relevant decisions: ADR-0001, ADR-0005–0006.
