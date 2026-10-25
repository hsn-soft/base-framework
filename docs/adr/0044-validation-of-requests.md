# ADR-0044: Validation of requests: attributes, rules and the messages for the client

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

The old system made every DTO field nullable and checked them by hand in `IValidatableObject.Validate`, to get its own messages and its own rules. Moving to the attributes of `System.ComponentModel.DataAnnotations` loses three things if nothing is done:
the texts are English only; a `Guid`, a number or a bool that is missing or is not valid is not found by `[Required]` (the model binder fills in the default, or fails with a JSON error that names a .NET type); and a rule that depends on other fields or on the data has no place.
The client also needs all that is wrong at once (3 fields of 10), in its own language, not the first error only.

## Decision

- **Attributes are the first choice** (`Required`, `StringLength`, `Range`, `EmailAddress`, ...). Their messages are translated: the web api matches the message an attribute made to the attribute, and tells it from the framework text `Validation:<Name>` in the language of the request
  (en, tr, ru, in the framework JSON; a test keeps the three in step). A string of white space is not a value for `[Required]` already; a value type that must be sent is declared nullable (`Guid?`, `decimal?`, `bool?`) with `[Required]`, so "missing" can be told from `0`/`false`; `[NotDefault]` (in the framework) refuses the default of the type (`Guid.Empty`, `0`) as an invalid value, so a client that sends the empty guid is told in the DTO, not by a 404.
- **A value that cannot be read** (not a guid, not a number, a broken JSON) is told as `Validation:InvalidValue` for its field, or `Validation:InvalidJson` for the whole body, never as the .NET message.
- **A rule that an attribute cannot say** is one of two things: in a DTO, `IValidatableObject.Validate` returning `ValidationRule.Failed(member, key, args)` (a result that names a text, translated like an attribute; it runs only when the properties are valid, as MVC does);
  in an application service, a `ValidationErrors` collector (`Add`, `AddIf`, `ThrowIfAny`) that throws one `ValidationFailedException` with every error, so the business rules also answer with a list.
- **The answer is a 400 with `errorCode: validation_error`; there is no new member in the envelope and no `payload`.** `statusMessages` has "Validation failed." and then one translated message for each wrong field (the label of the field inside the text).
  It is the same in every environment: these texts are written for the client, not diagnostics. A failure carries no payload; the details are in the messages.
- **The label of a field** is the translated `Field:<Dto>:<Property>`, then `Field:<Property>`, then the property name.

## Consequences

- A DTO needs no hand-written validation for the usual rules, and the own rules keep a place and the translations.
- Create and update DTOs of a service use nullable types for what must be sent; the application service takes `.GetValueOrDefault()` after the model is valid, and the domain still guards its own invariants (`Guard`, ADR-0039).
- The mapping of an attribute to its text is by its message, so a custom `ErrorMessage` on an attribute is shown as written.

## Related

[ADR-0038](0038-localization.md), [ADR-0039](0039-guard-and-translating-a-response-body.md), [ADR-0001](0001-record-architecture-decisions.md).
