# ADR-0048: Each sample has its own solution

- **Status:** Accepted
- **Date:** 2026-10-31 (replaces the one solution of [ADR-0001](0001-the-samples-are-self-contained.md))

## Context

`samples/Samples.sln` listed the projects of both samples (Abc and Edx). A person who works on one of them opened, restored, formatted and built the other as well, and a change in one could stop the check of the other.

## Decision

- **`samples/abc/Abc.sln` and `samples/edx/Edx.sln`**, each with the projects of its sample only (in the solution folders that mirror the directories). `Samples.sln` is gone. The framework projects stay out of both, as they stayed out of `Samples.sln`: they are references of the sample projects.
- **`samples/check.sh`** checks one sample (`check.sh abc`, `check.sh edx`) or both (no argument): restore, the format, and a Release build with warnings as errors, for each solution.
- A new project is added to the solution of its sample.

## Consequences

- One sample can be opened, built and checked without the other.
- The two samples share no project; a project that both needed would be a framework package or be copied, as it has been.

## Related

[ADR-0001](0001-the-samples-are-self-contained.md).
