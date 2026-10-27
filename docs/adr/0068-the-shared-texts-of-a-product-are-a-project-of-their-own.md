# ADR-0068: The shared texts of a product are a project of their own

- **Status:** Accepted
- **Date:** 2026-10-26 (follows [ADR-0038](0038-localization.md) and [ADR-0028](0028-shared-hosting-projects.md); the production system keeps them in `Hhs.Shared.Localization`)
- **Note:** [ADR-0072](0072-a-host-chooses-its-localization-and-its-cache.md): the host makes one `AddBaseLocalization` call with the shared texts and its own (no longer before `AddAbcMicroservice`, which does not touch the localization) and calls `UseBaseLocalization()` itself.

## Context

The texts a service says come in three layers, merged with the innermost first ([ADR-0038](0038-localization.md)): the framework's (`Status:*`, `Validation:*`), what several services of the product say, and the errors of each service. In the Abc sample the middle layer was two files of
`Abc.Shared.Hosting` (`Localization/{en,tr,ru}.json`), a project about the composition of the hosts, with two texts. The production system has a project for it, `Hhs.Shared.Localization`, so a language file of the product is found in one place and changed without touching the hosts.

## Decision

- **`Abc.Shared.Localization` is a project of its own** under `samples/abc/shared`: the shared `Localization/{en,tr,ru}.json` (embedded) and `AbcSharedLocalization.AddAbcSharedTexts(this LocalizationOptions)`, which adds them as a layer. It references only the framework's localization package.
  A text goes there when more than one service (or a host that is not a service) says it; a text of one service stays in the `Localization` of that service's Domain project.
- **The hosting projects do not reference it**: `Abc.Shared.Hosting` is about composing a host and a host may want the texts or not. A host that wants them adds the reference and asks for the layer itself, **before** it asks for the hosting, so the order of the layers stays
  framework, shared, service (`AddBaseLocalization` can be called more than once, each call adds its layer after the earlier ones): `services.AddBaseLocalization(configure: options => options.AddAbcSharedTexts())` first in the chain. For a microservice the reference is in its **Domain project**
  (next to its own `Localization`; the host reaches it through the Application project); the auth server references the project itself; the gateways do not need these texts and do not have them. The files moved with their history (`git mv`); the texts did not change (`status_required`, `invalid_credentials`).
- The three layers are now three places: the framework's package, `Abc.Shared.Localization`, the Domain project of each service. Xyz keeps its own layout.

## Consequences

- A new language file or text shared by the Abc services is one place to edit. A host that does not want the texts does not get them (the gateways' outputs do not carry the project); a host that wants them says so in its `Program.cs`, one line.
- One more small project in the sample (its own entry in the solution).
- Nothing was dropped from the old Hhs project: its helper `LocalizedModelValidator` is what `Validation:*` and the `[Required]` family do in the framework now, and its general answer texts are the framework's `Status:*`.

## Related

[ADR-0038](0038-localization.md), [ADR-0028](0028-shared-hosting-projects.md), [ADR-0002](0002-one-package-one-capability.md).
