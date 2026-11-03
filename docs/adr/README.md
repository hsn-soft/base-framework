# Architecture decision records

The ADRs explain **why** the repository is the way it is. Feature reference documents in [`docs/`](../README.md) explain **how** to use it.

## Find the relevant decisions

Start with the task area; read only the listed ADRs that affect the change. Use the [full ADR catalog](INDEX.md) to find additional decisions by topic. Do not read every ADR for an unrelated task.

| Task | Start with |
|---|---|
| Package boundaries, dependencies, or code location | [Overview](../Overview.md), ADR-0002–0006, ADR-0070, ADR-0075 |
| Build, tests, formatting, hooks, or CI | [Contributing](../../CONTRIBUTING.md), [CI detector](../../scripts/ci-detector/README.md), ADR-0007–0014, ADR-0109, ADR-0123 |
| A feature or package behavior | Its reference document in [`docs/`](../README.md), then the related ADRs listed in the [catalog](INDEX.md) |
| Branches, pull requests, or releases | [Releasing](../Releasing.md), [Contributing](../../CONTRIBUTING.md), ADR-0115–0116, ADR-0130, ADR-0132 |
| A sample (`abc`, `edx`, `hsx`) | [`samples/CLAUDE.md`](../../samples/CLAUDE.md); its decisions are [`samples/docs/adr/`](../../samples/docs/adr/README.md), numbered on their own |
| Adding a package or repository folder | [Contributing](../../CONTRIBUTING.md), ADR-0006, and the relevant package rules above |

Before changing code, read this guide and the ADRs relevant to the task. Avoid loading unrelated reference documents or the full ADR catalog unless you need to search for a decision.

## Writing a new ADR

Record a new architecture decision in the same change that makes it. Copy [the template](0000-template.md) to the next `NNNN-short-title.md`, then add a row to the [full catalog](INDEX.md). See [ADR-0001](0001-record-architecture-decisions.md) for what deserves an ADR and how to supersede one.
