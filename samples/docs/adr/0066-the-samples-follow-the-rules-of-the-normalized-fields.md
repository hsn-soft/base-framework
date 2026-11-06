# ADR-0066: The samples follow the rules of the normalized fields

- **Status:** Accepted
- **Date:** 2026-11-05

## Context

The framework decided how a `Normalized...` field is made and kept (framework ADR-0149 and ADR-0150): the Turkish letters are changed to English ones and only `A-Z`, digits and `@ . - _ +` stay; a normalized field is never empty; a duplicate is refused before the database; a query matches an `Id` or a normalized field. The samples are the pattern a service is copied from, so they must show it.

## Decision

- **Every entity of the three samples that makes a normalized field refuses an empty one where it makes it** (`Guard.NotNullOrWhiteSpace(StringHelper.Normalize(text), nameof(NormalizedX))`; the source text was already required).
- **Every EF Core entity configuration that has a normalized column has the check `"<Column>" <> ''`** (`CK_<Entity>_<Column>_NotEmpty`, the same text on PostgreSQL and on SQL Server). The document stores (Abc shipment and product, the Edx invoice, the Hsx text normalizer) have no check: the entity guard is their guard. Hsx has the migration `NormalizedNeverEmpty` in its identity and content services. **Abc and Edx make their tables with `EnsureCreated`, which does not change a table that is there: a development database made before this change is dropped and made again.**
- **A repository method takes a normalized text and says so in its parameter, and the application layer normalizes the input**: `IUserRepository.FindByUserNameAsync(normalizedUserName)` (Abc), `FindForSignInAsync(normalizedTenantCode, normalizedUserName)` (Edx) and `ISignInRepository.FindByEmailAsync(normalizedEmail)` (Hsx); the auth server of each sample normalizes what the client typed.
- The unique sets stay what each sample decided (the Abc user name is unique in the system, the Edx and Hsx user name in the tenant, and so on); what is added is that each set is the one the service looks for a duplicate in.

## Consequences

An empty normalized text cannot be stored by the sample entities, and a lookup by a typed text cannot reach a repository. A sample that is copied carries the rule with it. The samples have no tests; the rule is read in the entities, the configurations and the repositories.

## Related

Framework ADR-0149 and ADR-0150 (`../../../docs/adr`), [ADR-0065](0065-the-samples-store-an-enum-by-its-number-in-their-relational-databases.md).
