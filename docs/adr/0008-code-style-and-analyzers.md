# ADR-0008: Code style: the `.editorconfig` is the only source, analyzers run in the build

- **Status:** Accepted
- **Date:** 2026-10-22

## Context

Editors (Visual Studio, Rider, VS Code) and people format differently. Style discussions in reviews and style-only changes in diffs waste time, and a rule enforced only in the IDE
does not exist for CI.

## Decision

- **The `.editorconfig` is the single source of truth** for every editor: 4 spaces (2 for JSON), LF, UTF-8, a final newline, no trailing whitespace. Nobody configures a formatter by hand.
- **The rules run in the build** (`EnforceCodeStyleInBuild`, `AnalysisLevel latest-recommended`), so CI sees what the IDE sees. Rules that matter in this repository:
  - explicit types instead of `var` (except where the type is obvious on the right-hand side, `new Foo()`);
  - private static fields start with `s_` (`s_defaultOptions`), private/internal instance fields with `_`, constants are PascalCase;
  - library code has explicit `using` directives (no implicit usings), `ConfigureAwait(false)` on awaits, `ArgumentNullException.ThrowIfNull` on public entry points;
  - logging through `[LoggerMessage]` source generators (placeholders match the parameter names; event names are unique);
  - every public extension method that registers services has an XML comment saying what it registers and which configuration section it reads;
  - unused usings, members and parameters are warnings (and warnings are errors in CI).
- **Fix a warning, do not suppress it.** A narrow `#pragma` with a reason is allowed where the rule does not fit (a pass-through logger whose template is the caller's, a test stand-in for a third-party type);
  a project-wide `NoWarn` for code analysis is not.
- **Formatting is automatic:** the pre-commit hook formats the staged files and CI verifies (see [ADR-0013](0013-git-hooks.md), [ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md)).
  `scripts/ci-detector/format-json.cs` formats JSON the same way on every platform with only the .NET SDK.
- Test names say what is expected (`MissingConnectionString_IsAnError`); one behaviour per test.
- No secrets, tokens or real connection strings in the repository.

## Consequences

- A change that adds a public API comes with its XML comment or the build says so.
- New analyzer rules from an SDK upgrade can fail the build; they are fixed once, in the code.
- The rules are on `suggestion`/`info` severity in the file but are enforced because the hook and CI run `dotnet format style --severity info`.

## Related

`.editorconfig`, `CONTRIBUTING.md` (Code style and formatting), [ADR-0007](0007-build-settings-and-versioning.md), [ADR-0013](0013-git-hooks.md).
