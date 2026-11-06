# ADR-0137: The helpers of the old `Core` come back only when a consumer used them: text keys in `Core`, the rest in `Extensions`

- **Status:** Accepted
- **Date:** 2026-11-03

## Context

The old `HsnSoft.Base.Core` was a kitchen sink: about forty extension and helper classes, 19 KB of string extensions alone ([the analysis](../old-framework/1-foundation.md)). The new `Core` is small on purpose ([ADR-0004](0004-core-once-and-the-dependency-graph.md)), and the helpers were not brought back. Measured against the hhs system (calls to the helpers by their qualified names, the names of the .NET base library left out), only a handful were really used: `StringHelper.ReplaceInvalidChars` (30), `SlugKeyNormalize` (6), `Base64Encode`/`Base64Decode` (7), a filter `Or` (5), `JoinAsString` (2); the rest had no call. The old `ReplaceInvalidChars` cast each `char` to a `byte` (so `Ł` passed as `A`) and, called before the marks were taken off, turned `ş` into the replacement.

## Decision

- **A helper of the old framework comes back only when a consumer used it, and then written again with tests.** The ones with no call stay dropped: the DI marker interfaces, `PredicateBuilder`, `ToMd5`, `FirstCharCapitalize`, the file helpers, the Newtonsoft and `Json` helpers, the date and stream extensions and the rest of the extension classes.
- **Text keys are in `Core`**, in `StringHelper` beside `Normalize` and `Minimize` (one class, one package): `ReplaceInvalidChars` keeps the ASCII letters, digits, `-`, `.` and `_` (and `+`, `@` for an e-mail) and replaces every other character; `SlugKeyNormalize` takes the marks off first (`Çanakkale` becomes `canakkale`), maps the letters that do not decompose (`ı`, `ł`, `ø`, `đ`) and then replaces and trims, so a name with letters of the alphabet of its language gives a readable key. Keys made by the old code from names with such letters are different (`ş` was a replacement).
- **`FilterBuilder<T>` has `Or`** (the filter so far OR the part) and `FilterBuilder.AnyOf` (the alternatives put together to go into `And`), both leaving out a part that is `null`.
- **`HsnSoft.Base.Extensions` is the package for helpers that are not about one framework concept**, and it references `Core` and nothing of anyone else's: `Base64Text` (standard and URL-safe, with the `Try` variants) and a few collection extensions (`JoinAsString`, `WhereIf`, `IsNullOrEmpty`, `GetOrAdd`). A new helper goes in only with a consumer that uses it.

## Consequences

The two helpers hhs used most are in the package that already had their siblings, and the new package is small on purpose. A product that used the old names finds them with their new behavior; a name that had no consumer is not there, and the analysis says what to write if one is needed.

## Related

[ADR-0002](0002-one-package-one-capability.md), [ADR-0004](0004-core-once-and-the-dependency-graph.md), [ADR-0005](0005-where-code-lives.md), [the old framework analysis](../old-framework/1-foundation.md).
