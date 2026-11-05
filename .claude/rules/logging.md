---
paths:
  - "src/HsnSoft.Base.Logging*/**"
  - "docs/Logging.md"
  - "docs/LogLevels.md"
  - "docs/GraylogFields.md"
---

# Logging architecture

- Use `ILogger` throughout the framework. Keep provider implementations separate from the provider-neutral `HsnSoft.Base.Logging` contracts and options.
- Preserve the established log taxonomy: the writer's namespace determines its log type. Follow the existing request/message chain identifiers and log each failure once.
- Keep console verbosity (`Logging:LogLevel`) separate from the framework's persisted logging (`PersistentLogger`). Debug/Trace are not persisted by the framework's configured persistent sinks; do not assume product-added sinks follow those levels.
- Treat sensitive data as metadata on the model with `[SensitiveData]`, never as a setting-specific masking list. Do not log secrets or raw sensitive request/message bodies.
- When changing a log type or fields, update the measured field reference and its generation/tests as appropriate; do not invent field names without checking the measured map.
- Read [Logging.md](../../docs/Logging.md), [LogLevels.md](../../docs/LogLevels.md), and [GraylogFields.md](../../docs/GraylogFields.md) for feature detail before changing behavior.

Relevant decisions: ADR-0015–0021, ADR-0076, ADR-0124.
