# ADR-0123: The pre-commit style format builds the solution first

- **Status:** Accepted
- **Date:** 2026-11-01 (adds to [ADR-0107](0107-the-scripts-of-the-ci-and-the-detector-live-in-one-folder.md))

## Context

The pre-commit hook formats the staged C# files with `dotnet format style --no-restore`. Several commits of a working session lost `using` lines of the sample hosts and no longer built: the hook had removed them. It was reproduced: with the
`bin` and `obj` folders of a sample gone (after the CI detector, a clean, a fresh clone) `dotnet format` cannot resolve the namespaces of the projects the sample references, takes every `using` of them for unnecessary (IDE0005) and removes it.
After a build the same command leaves the file alone.

## Decision

`scripts/ci-detector/format-staged.py` builds the solution that owns the files (a restore and a build, a no-op where nothing changed) before the `style` and `verify-style` modes, and formats nothing when it does not build: it says so and stops
the commit, so a `using` is never removed on the strength of a broken or missing build. The `whitespace` mode needs no references and does not build.

## Consequences

- A commit never removes a `using` that is needed; a clean tree commits correctly.
- A commit of a C# file in a solution that does not build is refused until the build is fixed (the message has the last lines of the errors).
- A commit takes the time of a build of the solutions it touches (seconds when built before).

## Related

[ADR-0107](0107-the-scripts-of-the-ci-and-the-detector-live-in-one-folder.md).
