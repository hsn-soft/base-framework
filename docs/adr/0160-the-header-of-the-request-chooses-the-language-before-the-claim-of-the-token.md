# ADR-0160: The header of the request chooses the language before the claim of the token

- **Status:** Accepted; supersedes the order of the language of a request in [ADR-0038](0038-localization.md)
- **Date:** 2026-11-17

## Context

ADR-0038 put the `locale` claim of the token before the `Accept-Language` header. The claim is the language the user chose once, in the profile, and it lives as long as the token; the header is what the caller asks **for this request** (the back office sends the language of its screen on every call). With the claim first, a user whose profile says `tr` could never see a screen of the app in `ru`: the answer to a call from that screen came back in the profile language whatever the header said. The claim is a default of the user, not an answer to the request.

## Decision

The language of a request is, in this order:

1. the supported language of the **`Accept-Language` header** (the highest quality first; `tr-TR` is `tr`);
2. the **`locale` claim** of the token (`Localization:LanguageClaim`), when it is a supported language;
3. the **default language** (`Localization:DefaultLanguage`, `en`).

A value that is not in `Localization:SupportedLanguages` (`en`, `tr`, `ru`) is the same as no value, in the header and in the token: a header that names only `de` is no header and the token counts; a token with `de` is no token and the default counts. The header may name several languages: the first supported one by quality wins, `q=0` is skipped.

## Consequences

A call that sends `Accept-Language` is answered in that language whatever the profile of the user says; a call without one (a script, a token used from a tool) is answered in the language of the user; a call with neither is answered in English. The handlers of the outer layers (the exception handler, the status pages) use the same resolver, so the order is the same everywhere. The services get it with the next release of the framework package; they have no code of their own for it. Nothing in the settings changes.

## Related

[ADR-0038](0038-localization.md) (the rest of it stands: the languages, the fallback to the default language, the layers), `docs/Localization.md`, `RequestLanguage.cs` (`RequestLanguageResolver`), `LocalizedAnswersTests`.
