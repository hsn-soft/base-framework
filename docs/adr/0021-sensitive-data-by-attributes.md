# ADR-0021: What is sensitive is said by the classes, never by settings

- **Status:** Accepted
- **Date:** 2026-10-23

## Context

Logs carry request and response bodies, messages and objects. A list of sensitive keys kept in settings drifts, differs per service and is wrong the day a new property is added; the old services
hid values by a list of names. Masking must also not depend on where the log goes (the console is a log too).

## Decision

- **A property that must not appear in a log is marked `[SensitiveData]`** on the property of the class (`HsnSoft.Base.Logging.Masking.SensitiveDataAttribute`, in Core). The class is the one source: the attribute says whether the value is hidden entirely or partly.
- **`ILogMasker`** (`HsnSoft.Base.Logging`) masks a JSON text (`MaskText(text, shape)`: besides the usual keys such as passwords and tokens, the properties of that class and of the classes inside it that carry the attribute) and an
  object (`MaskObject`). The logging provider masks objects logged with `{@Object}` (a destructuring policy); the request/response bodies, the `MessageBody` of a message and the logged objects use the masker.
- A place that sees only JSON and not the classes (a **gateway**) hides the properties of the contract classes of the assemblies it is told about: `AddBaseLogging(typeof(SomeContract).Assembly)` builds the registry.
- **Never from settings:** there is no configuration list of sensitive keys.
- Masking is independent of the destination: it happens before the log is written, so the console, the file and Graylog all get the masked text. When no `ILogMasker` is registered, a body or a message is **not logged at all** rather than logged
  unmasked.
- The raw body switch of the event bus (`EventBusOptions.LogMessageBody`) is off by default and is for debugging only.

## Consequences

- A new sensitive property is protected by marking it, in the class, in the same change.
- A third-party class cannot carry the attribute; wrap it or exclude it from the log.
- Tests: masking by attribute at a service and at a gateway (registry), objects with `{@Object}`, and the absence of a masker.

## Related

[ADR-0018](0018-chain-logs.md), `docs/Logging.md`, `SensitiveDataAttribute`, `LogMasker`, `SensitiveDataRegistry`.
