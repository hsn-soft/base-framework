# ADR-0038: Localization: embedded JSON texts, merged from the innermost layer out, in the language of the request

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

The framework before the rewrite localized with JSON files embedded in the assemblies (`{ "culture", "texts" }`), merged into one dictionary per culture (the framework's, then the service's), and a middleware that read only the `Accept-Language` header. It worked but: the cache key of the multi-resource localizer used only the first resource (two sets with the same first resource shared a dictionary),
the JSON was read again after 30 seconds, the header was cut by hand without qualities, there was no fallback chain, the language claim of the token (`user_lg`) was written and never read, the domain exceptions took an `IStringLocalizer` in their constructors, nothing checked that a translation was complete, and the choice of the texts went through `CultureInfo`, which does not work where the runtime has
no culture data (a container with `InvariantGlobalization`).

## Decision

- **A new package, `HsnSoft.Base.Localization`**, with the contracts in Core (`ILanguageAccessor`, `IMessageLocalizer`, `ILocalizableError`). It does not bring back `HsnSoft.Base.VirtualFileSystem`: reading a text embedded in an assembly is a few lines inside the package.
- **Texts are JSON files embedded in the assemblies**, one per language in a folder named `Localization` (`Localization/en.json`, `tr.json`, `ru.json`; `<EmbeddedResource Include="Localization\*.json" />`), in the old form `{ "culture": "tr", "texts": { "key": "text" } }`. A key twice in a file, a `culture` that is not the file's language, or a file that cannot be read stops the application when it starts.
- **Layers are merged from the innermost out into one dictionary per language**, built once and never changed: the framework's own texts (the status messages and the validation message of the web api answers) are the outermost layer, then the shared project (`Abc.Shared.Hosting`), then the service's domain. **A later (more inner) layer replaces the text of the same key of an outer one.** Layers are registered outermost first (`AddBaseLocalization(configure: o => o.AddEmbedded(assembly))`, it can be called more than once).
- **Languages: `en`, `tr`, `ru`** (the `Localization:SupportedLanguages` setting; the default language is `en`, `Localization:DefaultLanguage`). A text missing in the current language falls back to the default language, then to nothing (a key that no language has is logged once as a Warning). The language is a **short language code**, not a `CultureInfo`: `tr-TR` is `tr`.
  At start the translations are checked against the default language: a key a language lacks, a key only a language has, other `{n}` places, a layer without a language: each is a Warning.
- **The language of a request is, in this order: the `locale` claim of the token (the name is `Localization:LanguageClaim`), then the `Accept-Language` header (the highest quality first, the languages that are not supported skipped), then the default language.** `UseBaseLocalization` sets it for the rest of the pipeline and answers `Content-Language`; it goes after the authentication. The handlers of the outer layers (the exception handler, the status pages) work out the language from the request themselves, as what the middleware sets does not reach them.
- **Only what goes to the client is translated; the logs say what they always said.** An error that carries a key (`ILocalizableError`: `WithKey(key, args)`; for a `BusinessException` the error code is the key unless another is given) is told in the language of the request with the arguments put in the `{0}`, `{1}` places; without a text for the key the client gets the exception's message. The status messages, the `Error code:` line and the validation message are texts too. The failures of the service (5xx) stay the codes they are.
- **The domain stays free of localization:** an exception names the key and the arguments, it never asks for a text. A service has its keys in `Localization/*.json` of its domain project and gives the assembly to `AddAbcMicroservice`.
- **In the Abc system** a user has a `Language` (`admin` is `en`, `alice` is `tr`), which the auth server puts in the token as `locale`.

## Consequences

- A host without `AddBaseLocalization` answers as before (English); the localization is opt-in and the web api package does not reference it.
- A text is changed by a build (no folder of texts is read at run time); a mounted folder can be added later as another layer.
- Texts of DataAnnotations attributes (`[Required]`) are not translated yet: the validation answer says "Validation failed." in the language, the fields' own messages are the framework's. Names of fields in a message are the property names.
- The enum values of a DTO are stable codes (`Pending`); the labels are the client's.

## Related

[ADR-0033](0033-the-abc-services-crud-pattern.md), [ADR-0037](0037-audit-soft-delete-and-tenants.md), [`docs/Localization.md`](../Localization.md), [`docs/WebApi.md`](../WebApi.md).
