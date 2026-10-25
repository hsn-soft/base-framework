# ADR-0041: `ApplicationService` is the framework's; the application services of a service derive from its base

- **Status:** Accepted
- **Date:** 2026-10-25 (extends [ADR-0040](0040-api-controller-base-in-the-framework.md) to the application layer; replaces the `IMessageLocalizer` and `ICurrentUser` of the Abc application services of [ADR-0039](0039-guard-and-translating-a-response-body.md))

## Context

The controller has `Logger`, `CurrentUser` and `CurrentTenant` from the framework's base (ADR-0040) and `L` from the service's. The application services asked for what they needed in their constructors (`ICurrentUser`, `IMessageLocalizer`) and had no logger, so each one repeated the same parameters.

## Decision

- **`HsnSoft.Base.Application.ApplicationService` (Core)** is an abstract class with `Logger`, `CurrentUser`, `CurrentTenant` and `ServiceProvider`, found from the `IServiceProvider` it is given the first time they are used: the same idea as `ApiControllerBase`. An application service passes the provider up the chain
  (`class OrderAppService(IOrderRepository orders, IServiceProvider services) : ApplicationServiceBase(services), IOrderAppService`); the dependency injection gives the provider to any constructor that asks. It is in Core because an application service does not know HTTP.
- **Each service has an `ApplicationServiceBase : ApplicationService`** in its application project, with **`L`** (the standard `IStringLocalizer`), as its base controller has: `L["OrderStatus:Pending"].Value` translates a text of a response body into the language of the request. The application project references `Microsoft.Extensions.Localization.Abstractions` for it.
  Everything else (repositories, other services) is still injected in the constructor.

## Consequences

- An application service has no `ICurrentUser` or localizer parameter; a worker (no request) has no current user, and `L` then answers in the default language.
- The service locator is only for these four things; anything a use case depends on stays visible in its constructor.
- `IMessageLocalizer.Get` stays for code that has no `L` (Core, a domain service).

## Related

[ADR-0040](0040-api-controller-base-in-the-framework.md), [ADR-0039](0039-guard-and-translating-a-response-body.md), [ADR-0036](0036-icurrentuser-in-the-framework.md).
