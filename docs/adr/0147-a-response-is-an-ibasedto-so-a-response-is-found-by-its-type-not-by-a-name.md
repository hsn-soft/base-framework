# ADR-0147: A response is an `IBaseDto`, so a response is found by its type and not by a name

- **Status:** Accepted
- **Date:** 2026-11-04

## Context

A service has the rule that what reaches a client is a DTO (a controller answers `Task<...Dto>`, and a response holds values and other DTOs, never an entity). To check it by reflection the code had to guess what a DTO is: a name that ends with `Dto`, a namespace of a `Contracts` folder. A class of that name in another place, or an entity named `...Dto`, passed; and every project that wanted the check wrote the same guess.

## Decision

`HsnSoft.Base.Ddd.Application` has the marker `IBaseDto` (`HsnSoft.Base.Application.Dtos`) for **what a service sends**: the response. The response bases of the framework have it already: `IEntityDto<TKey>` (so every `EntityDto`, `AuditedEntityDto` and the like) and `IListDataResult<T>` (so `ListDataResultDto<T>` and `PagedDataResultDto<T>`). A response of a service with another shape (one that has no id, `SampleDto : IBaseDto`) implements `IBaseDto` by itself.

**What a client sends (a submit, a filter, a list request) does not have to be an `IBaseDto`, and no rule or test holds it to it.** A project that wants to hold a rule over its responses says it with the type: a response is an `IBaseDto`; what a controller returns, and what a response has as a property, is a value or an `IBaseDto`, never an entity. That the name ends with `Dto` is a naming rule of its own, kept by a test.

## Consequences

A test, or a runtime filter of a host, can refuse a response that is not an `IBaseDto` without a list of names or folders. The marker has no member, so it costs a line in a response that has no framework base. The parts of an integration event are not API responses and do not have to implement it.

## Related

[ADR-0042](0042-ddd-domain-and-application-packages.md), [docs/DddBuildingBlocks.md](../DddBuildingBlocks.md).
