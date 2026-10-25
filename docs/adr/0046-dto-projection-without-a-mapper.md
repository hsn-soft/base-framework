# ADR-0046: A DTO is read by a projection made from the DTO, without a mapper

- **Status:** Accepted
- **Date:** 2026-10-25 (extends [ADR-0042](0042-ddd-domain-and-application-packages.md), which left the mapper for later)

## Context

A list screen needs a few fields of an entity. Loading the entity and copying fields by hand (`ToDto`) reads every column and every owned collection; writing the `Select` by hand for each DTO is the work a mapper (AutoMapper's `ProjectTo`) exists for. A mapper is a package with a
history of surprises (a configuration that fails at runtime, a mapping that does not translate to a query), and the old system needed it only for this.

## Decision

- **The `Select` is made from the DTO** (`EntityProjection.For<TEntity, TDto>()`, built once for each pair): each public property of the DTO with a setter is filled from the entity's property of the same name (case does not matter); a name that goes through objects is a path
  (`CustomerName` is `Customer.Name`); an object or a list of objects is projected member by member into the DTO's own type (array, `List`, `IReadOnlyList` or any type a `List` fits). A property that must not be filled has `[NotMapped]`.
- **A DTO property that nothing answers is an error that names all of them,** when the selector is first needed, not a silent `null` (and so is a DTO without a constructor without parameters, or a value that cannot be converted).
- **It is an expression, so the store does the work:** a relational store reads only the columns of the DTO and joins what it reaches (no `Include`); a document store projects only its fields.
- **The repositories offer it as `FindAsync<TDto>`, `GetListAsync<TDto>`, `GetPagedListAsync<TDto>`,** with an optional `selector` for what a name cannot say (a calculation, a format).
- **Writing stays by hand:** a DTO becomes an entity through the entity's own methods, which keep the rules of the domain. No mapper is used for it.

## Consequences

- A service writes `GetPagedListAsync<ProductListDto>(options)` and gets one query with the columns it needs.
- A DTO that cannot be built from its entity is found by the first call (and by a test that calls it), not in production.
- A mapper may still be chosen later for a case this does not cover, as a separate decision.

## Related

[ADR-0045](0045-repository-levels-relational-and-document.md), [ADR-0042](0042-ddd-domain-and-application-packages.md).
