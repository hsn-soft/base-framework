# DTO projection: how a DTO is filled from an entity

A read method of a repository that names a DTO (`FindAsync<TDto>`, `GetListAsync<TDto>`, `GetPagedListAsync<TDto>`) does not load the entity and does not map after the read. It builds a `Select` from the DTO ([ADR-0046](adr/0046-dto-projection-without-a-mapper.md)), so the database returns only the columns the DTO has, and the limit and the offset are in the same command. The rules live in `EntityProjection` (`HsnSoft.Base.Domain.Repositories`).

## The rules (each public property of the DTO that has a setter)

| The DTO property | Is filled from |
|---|---|
| `Domain` | the entity's property `Domain` (same name, case does not matter) |
| `CompanyName` | a path through objects: `entity.Company.Name`, i.e. the `Name` column of the table the `Company` navigation points to. The read joins that table and takes that one column; the company is not loaded and no `Include` is needed |
| `Company` (a nested DTO) / `Lines` (a list of DTOs) | the object or the list, projected member by member by the same rules |
| `[NotMapped]` | nothing: the service fills it after the read (the names of ids of **another** service, from Redis lookups: `ILookupReader` of the shared library) |

A path is the names of the navigations joined with the name of the last property: `CompanyName` is `Company` + `Name`; `CompanyOwnerEmail` would be `Company` + `Owner` + `Email`. The entity must have the navigation (`public Company? Company { get; private set; }`) and the EF configuration must map it (`HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId)`).

## When a DTO property has nothing that answers it

`CompanyAddress` on a DTO, when neither the entity nor `Company` has `CompanyAddress` or a path that spells it: the first read that uses the DTO throws `InvalidOperationException` (*"CustomerDto cannot be filled from Customer: nothing answers CustomerDto.CompanyAddress. Name them as the entity's properties (or as a path ...), mark them [NotMapped], or give the selector yourself."*). The selector is built once for the pair of types and kept, so the error is the same for every call and a test that reads the endpoint finds it. A property that is not mapped is **never** silently left at its default.

This is about the **mapping**, not about the **data**. When the mapping is right and the value is `null` in the database (an optional foreign key, so the join is a `LEFT JOIN` and there is no company; a nullable column), the DTO property is `null`. Declare such a property nullable (`public string? CompanyName { get; init; }`, as `AppRoleDto` does). A DTO property that is not nullable gets `null` all the same, because the select does not check it.

## A value the rules cannot say: write the selector

A calculation, a join of several columns or a format is not a rule. Give the selector yourself and **create the DTO with `new`, filling every field in it** (the rules are not applied to a selector you give, so a field you leave out stays at its default):

```csharp
IReadOnlyList<CustomerDto> items = await repository.GetListAsync(
    options,
    entity => new CustomerDto
    {
        Id = entity.Id,
        CompanyId = entity.CompanyId,
        Domain = entity.Domain,
        CompanyName = entity.Company!.Name,
        CompanyAddress = entity.Company!.Street + " " + entity.Company!.District + " / " + entity.Company!.City,
        CreatedAt = entity.CreatedAt, CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt, UpdatedBy = entity.UpdatedBy,
    },
    cancellationToken);
```

The selector is an expression: EF turns it into SQL, so it can use only what EF can translate (`+` on strings, `string.Concat`, `Coalesce`; not your own methods). It is still one command with a join. The same selector goes to the page method; the count is made without it.

## Writes: `ToDto` of an entity in memory

`EntityProjection.ToDto<TEntity, TDto>(entity)` makes the DTO of an entity that is already in memory (what `AddAsync` gave back), so a create or an update answers with no second read:

```csharp
private static CompanyDto ToDto(Company entity) => EntityProjection.ToDto<Company, CompanyDto>(entity);
```

Used by `CompanyAppService`, `TenantAppService`, `AppUserAppService` and `SubscriptionAppService` (identity) and by the content service. It works only for a **flat** DTO (`EntityProjection.IsFlat<TEntity, TDto>()`): every field is a property of the entity itself. A DTO with a path (`CompanyName`) or a list is not flat and `ToDto` throws, because what a path reaches is not in an entity that was just made. Then the service builds the DTO with `new` from the entity and the value it has already read (`CustomerAppService` reads the company name once, with the check that the company exists), or reads the DTO from the repository (`FindAsync<TDto>`).

## What to check when you add a DTO

- A DTO with a path or a nested object: the entity has the navigation and the configuration maps it.
- A property that has no counterpart is `[NotMapped]` (and filled by the service) or the selector is written.
- An optional relation gives a nullable DTO property.
- The test that reads the list passes (it builds the selector), and for a list that matters, a test records the command and checks the columns, the `JOIN` and the `LIMIT` (see `CustomerListsTests` in the identity service).
