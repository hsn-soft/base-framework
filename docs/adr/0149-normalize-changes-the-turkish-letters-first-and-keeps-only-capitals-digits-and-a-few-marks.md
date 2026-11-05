# ADR-0149: `StringHelper.Normalize` changes the Turkish letters first and keeps only capitals, digits and a few marks

- **Status:** Accepted
- **Date:** 2026-11-05

## Context

`StringHelper.Normalize` is what every `Normalized...` column is made with (the twin of a text that a list searches and compares). It trimmed the text, made it upper case with the invariant culture and took the marks off the letters. That left holes: the dotless `ı` and the dotted `İ` do not decompose, so `ı` became `I` only by luck of the invariant case table and `İ` was kept as it is; a space stayed, so `"Tech Summus"` and `"TechSummus"` were two different tenants; any punctuation or letter of another alphabet stayed in a column that is meant to be compared and indexed.

## Decision

`Normalize` works in this order: (1) the letters are changed to their English letters: `ı` and `İ` become `i` and `I`, `öçşğü` and `ÖÇŞĞÜ` become `ocsgu` and `OCSGU`, any other mark of a letter is taken off (`Müller` is `MULLER`) and the letters that have no decomposition are mapped (`ł`, `ø`, `đ`, `ß`); (2) everything is in capitals; (3) **only `A-Z`, `0-9` and `@ . - _ +` stay**: a space, any other punctuation and a letter of another alphabet are left out. `null` and blank are empty. `Minimize` makes the first step the same (so `ı` is `i`) and keeps every other character; `SlugKeyNormalize` uses `Minimize`.

## Consequences

Two names that differ only in spaces, case or Turkish letters have the same normalized text, and a normalized column holds nothing but those characters. A normalized text is not readable as the name (`TECHSUMMUSA.S.`), so it is never shown, only compared. A service that stored the old form recomputes its normalized columns; a service that compares with `Normalize(input)` on both sides needs no change. A text that has only other alphabets (Chinese, Arabic) normalizes to empty and must not be used as a key.

## Related

[ADR-0042](0042-ddd-domain-and-application-packages.md), [ADR-0145](0145-an-enum-has-a-key-and-a-name-through-the-display-attribute.md), `src/HsnSoft.Base.Core/HsnSoft/Base/Text/StringHelper.cs`.
