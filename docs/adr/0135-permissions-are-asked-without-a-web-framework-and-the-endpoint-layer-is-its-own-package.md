# ADR-0135: The permission checker needs no web framework, and the endpoint layer is its own package (`Authorization.AspNetCore`)

- **Status:** Accepted
- **Date:** 2026-11-03 (refines [ADR-0081](0081-permissions-one-framework-package-and-the-grants-in-the-distributed-cache.md), which put everything in one package)

## Context

[ADR-0081](0081-permissions-one-framework-package-and-the-grants-in-the-distributed-cache.md) made one package, `HsnSoft.Base.Authorization`, for the whole of permissions. It carries a `FrameworkReference` to `Microsoft.AspNetCore.App` because the attribute `[PermissionAuthorize]`, the policy provider and the handler are ASP.NET Core types. The checker (`IPermissionChecker`), the store of the grants and the catalog are not: an application layer, a domain service or a worker that only asks "may the caller do this?" had to take the whole web framework to do it. That is the case the motto names (never reference a library the consumer does not use), and [ADR-0003](0003-abstractions-separate-from-providers.md) names the shape (`X.Y` is a layer on `X`).

## Decision

- **`HsnSoft.Base.Authorization` has no web framework.** It holds the checker, the store of the grants in the distributed cache, the grants, the catalog of permission codes, the scope provider and the options, and references `Core` and the Microsoft caching and options packages. `AddBasePermissionChecker(configuration)` registers the checker and the store; `AddPermissionScope<T>()` stays here.
- **`HsnSoft.Base.Authorization.AspNetCore` is the layer on it** for an endpoint: `[PermissionAuthorize]`, the policy provider, the requirement and its handler, and `AddBasePermissions(configuration)`, which calls `AddBasePermissionChecker` and then turns on the policies. It references `Authorization` and `Microsoft.AspNetCore.App`. The namespace of every type stays `HsnSoft.Base.Authorization`, so a `using` does not change.
- A host that protects endpoints references the AspNetCore layer (and gets the core with it, [ADR-0070](0070-a-framework-package-brings-what-its-api-exposes.md)); a layer that only asks permissions references the core.
- `RequireValueAsync` and its siblings throw a `BaseHttpException` with the number 403 instead of `StatusCodes.Status403Forbidden`, so the core needs no `Microsoft.AspNetCore.Http`.

## Consequences

An application, domain or worker project no longer takes ASP.NET Core to ask a permission. The packages and tests are two: `Authorization` / `Authorization.Tests` and `Authorization.AspNetCore` / `Authorization.AspNetCore.Tests`. The two sample hosting projects (Edx and Hsx) reference the AspNetCore layer; their application layers keep the core. A product that referenced `HsnSoft.Base.Authorization` for the attribute adds the AspNetCore package.

## Related

[ADR-0003](0003-abstractions-separate-from-providers.md), [ADR-0004](0004-core-once-and-the-dependency-graph.md), [ADR-0070](0070-a-framework-package-brings-what-its-api-exposes.md), [ADR-0081](0081-permissions-one-framework-package-and-the-grants-in-the-distributed-cache.md), [ADR-0083](0083-four-permission-types-and-constraints-that-must-be-given.md), [Authorization](../Authorization.md).
