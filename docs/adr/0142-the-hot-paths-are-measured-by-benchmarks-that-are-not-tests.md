# ADR-0142: The hot paths are measured by benchmarks that are not tests (`benchmarks/`)

- **Status:** Accepted
- **Date:** 2026-11-03 (adds a folder to the layout of [ADR-0006](0006-repository-folders-and-the-solution-file.md))

## Context

The motto of the framework says performant, and nothing measured it: no benchmark existed in the repository (the old framework had load workers that were disabled, [the analysis](../old-framework/5-tests-repo-graph.md)). A claim about speed (a masker that does not allocate more than it must, a sort that is checked against a list of fields, a message that is serialized once) is a number, and a number is only worth something next to the one before it.

## Decision

- **`benchmarks/HsnSoft.Base.Benchmarks`** is a console project (BenchmarkDotNet, not packable, in the solution folder `benchmarks`) with the paths that run on every request or message: masking of a log text and of an object, the serialization of a message envelope, the sort and the filter of a list, the text helpers (`Normalize`, `SlugKeyNormalize`), the address check and the path of a blob. A group is run with `dotnet run -c Release --project benchmarks/HsnSoft.Base.Benchmarks -- --filter "*Masking*"`.
- **Benchmarks are not tests and not part of the CI**: they take minutes, and their numbers depend on the machine, so they gate nothing. They are built (the Release build of the solution covers them, so they never rot) and run by a person before and after a change that touches one of these paths; the two numbers go into the message of the commit that changes the path.
- A path joins the project when a change makes a claim about its speed or allocations.

## Consequences

A claim about performance has a place to be checked and a way to be repeated. The cost is a folder and a package (BenchmarkDotNet, in the central versions) that no package of the framework references.

## Related

[ADR-0006](0006-repository-folders-and-the-solution-file.md), [ADR-0134](0134-package-versions-are-managed-centrally-and-a-library-awaits-with-configureawait-false.md), [the old framework analysis](../old-framework/README.md).
