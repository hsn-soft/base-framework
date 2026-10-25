# ADR-0039: `Guard` for the checks of the domain, and translating a text in a response body

- **Status:** Accepted
- **Date:** 2026-10-25 (adds to [ADR-0038](0038-localization.md))

## Context

The framework before the rewrite had `LocalizedModelValidator`, a static class with a static localizer that every product configured at start: each check built the same six lines (look the text up, fall back to English, throw a `DomainException` with the translated message), and the domain
asked for the text itself. A base controller also gave `L` (a localizer made of a list of resource types), `CurrentUser`, `CurrentTenant` and the others through an `IServiceProvider` passed to every controller. The application services and the controllers also translate texts that go into a response body (labels), which the exceptions
of ADR-0038 do not cover.

## Decision

- **`HsnSoft.Base.Guard` (Core)** replaces the validator: `NotNull`, `NotDefault`, `NotNullOrEmpty`, `NotNullOrWhiteSpace(value, name, max, min)` (returns the trimmed text), `Length`, `Range`, `NotNegative`. A failed check throws a `DomainException` with an English message (what the log says) and a key
  (`Validation:Required`, `IsNotEmpty`, `MaxLength`, `MinLength`, `Range`, `NotNegative`) whose first argument is the name of the parameter; the framework's own texts have them in `en`, `tr`, `ru`. There is no static state and no localizer: the host translates when it answers. The entities of the Abc services use it instead of the `Required` they each had.
  A message that is the domain's own (an order cannot change its status once delivered) is still its own key in the `Localization` of the domain project.
- **A text in a response body is translated where the body is made.** An application service asks for `IMessageLocalizer` (Core; `localizer.Get("OrderStatus:Pending")` gives the key when no language has the text, `Find` gives `null`) and a controller has **`L`**, the standard `IStringLocalizer`, in the base controller. Both read the one set of texts, in the language of the request. The Abc
  order and shipment responses have `StatusText` next to `Status` (the stable code the client reacts to).
- **The base controller of a service has `L`, `CurrentUser`, `CurrentTenant` and `Logger` as properties** found from `HttpContext.RequestServices` when first used: a controller has no constructor for them and no `IServiceProvider` to pass. Anything else is injected.
- The labels of fields in a message (the old `ErrorReference`, which translated the parameter name) are not done: the name goes in as it is. A product that needs them names the field with a key of its own.

## Consequences

- The checks of the domain are one line and the same everywhere; a new check is a new method with a key and three texts.
- The `Required`/`Validation:*` messages of the entities changed their English wording; the keys the Abc services had for them (`field_required`, `field_too_long`, `field_negative`, `password_length`, `order_line_*`) are gone.
- A response body that has a translated text depends on the language of the request, so such a response is not the same for two users: a cache of it must vary by language.

## Related

[ADR-0038](0038-localization.md), [ADR-0033](0033-the-abc-services-crud-pattern.md), [`docs/Localization.md`](../Localization.md).
