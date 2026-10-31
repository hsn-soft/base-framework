# ADR-0001: The samples are self-contained: their own solution, documents and decisions

- **Status:** Accepted
- **Date:** 2026-10-28

## Context

The sample systems (Abc, Edx) showed the framework at work, and their decisions, examples and comments had spread into the framework: ADRs about gateways and invoices sat beside the ADRs of the packages, the framework solution listed 62 sample projects, and the
docs named sample classes. A reader of the framework had to know which part was the product and which was a demonstration, and a change to a sample could break the framework's build and hooks.

## Decision

**`samples/` is written as if the rest of the repository did not know it.** Everything that concerns a sample is inside `samples/`; nothing outside it names a sample (the framework docs use generic examples).

- **Own solution:** `samples/Samples.sln` lists the sample projects (replaced by one solution for each sample, [ADR-0048](0048-each-sample-has-its-own-solution.md)); `HsnSoft.Base.sln` lists only `src` and `test`. The framework's CI, pre-push hook and detector do not build samples; `samples/check.sh` restores, verifies the format and builds them.
- **Own build settings:** `samples/Directory.Build.props` and `samples/Directory.Packages.props`.
- **Own documents:** `samples/docs/` (guides) and `samples/docs/adr/` (decisions, numbered from 0001 here, in the form of the framework's ADRs).
- **Own scripts and ignore rules:** `samples/scripts/`, `samples/.gitignore`.
- **One direction:** a sample may reference `src/` and link to framework documents (written *framework ADR-NNNN*); the framework never references a sample.

Decisions that were taken as framework ADRs but are about a sample were moved here and renumbered; the old number is in the table. Mixed ones (0081, 0083) were split: the framework keeps the rule, the sample keeps its own part.

| Framework number (before) | Number here |
|---|---|
| 0027 | 0002 |
| 0028 | 0003 |
| 0029 | 0004 |
| 0030 | 0005 |
| 0031 | 0006 |
| 0032 | 0007 |
| 0033 | 0008 |
| 0034 | 0009 |
| 0035 | 0010 |
| 0049 | 0011 |
| 0051 | 0012 |
| 0052 | 0013 |
| 0053 | 0014 |
| 0054 | 0015 |
| 0055 | 0016 |
| 0056 | 0017 |
| 0058 | 0018 |
| 0059 | 0019 |
| 0060 | 0020 |
| 0061 | 0021 |
| 0064 | 0022 |
| 0068 | 0023 |
| 0069 | 0024 |
| 0077 | 0025 |
| 0078 | 0026 |
| 0079 | 0027 |
| 0080 | 0028 |
| 0082 | 0030 |
| 0084 | 0032 |
| 0085 | 0033 |
| 0086 | 0034 |
| 0087 | 0035 |
| 0088 | 0036 |
| 0089 | 0037 |
| 0090 | 0038 |
| 0091 | 0039 |
| 0092 | 0040 |
| 0093 | 0041 |
| 0094 | 0042 |
| 0095 | 0043 |
| 0096 | 0044 |
| 0097 | 0045 |
| 0072 | 0046 |
| 0081 (sample part) | 0029 |
| 0083 (sample part) | 0031 |

## Consequences

- The framework can be read, built and released without the samples; the samples can be deleted without touching it.
- A sample decision costs a second place to look; the index in [README.md](README.md) is that place.
- The numbers of the framework have gaps (the moved ones); they are never reused (framework ADR-0001).

## Related

[framework ADR-0001](../../../docs/adr/0001-record-architecture-decisions.md), [framework ADR-0006](../../../docs/adr/0006-repository-folders-and-the-solution-file.md)
