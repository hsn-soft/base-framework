---
paths:
  - "src/HsnSoft.Base.Localization/**"
  - "docs/Localization.md"
---

# Localization

- Keep localized texts in the framework's embedded JSON resources and follow the existing resource naming and key conventions.
- Preserve the layer merge order: the most specific/innermost layer wins, with outer layers supplying fallback texts. Do not replace this with a global dictionary or runtime-only file lookup.
- Resolve language through the established request-language flow and preserve the client's stable message keys when translating errors.
- Keep text lookup provider-neutral and optional; do not add localization dependencies to packages that do not need them.
- Read [Localization.md](../../docs/Localization.md) and ADR-0038 before changing resource discovery, merge precedence, or language selection.

Relevant decisions: ADR-0002, ADR-0038–0039.
