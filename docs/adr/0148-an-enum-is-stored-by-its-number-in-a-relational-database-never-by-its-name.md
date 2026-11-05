# ADR-0148: An enum is stored by its number in a relational database, never by its name

- **Status:** Accepted
- **Date:** 2026-11-05

## Context

Entity Framework Core stores an enum property as its number unless the model is told to convert it (`HasConversion<string>()`, `EnumToStringConverter`). Converting to text had been copied from service to service because the value is readable in a table. It costs more than that gains:

- A name is a column of text: wider rows, wider indexes, a string compare on every filter and join, and a collation to think about. The number is a four-byte integer (a `smallint` when the type says so).
- A name is part of the data. Renaming an enum member (a change that is free in code) means migrating every row, and a rename that is forgotten makes the old rows unreadable at run time.
- The text is a second copy of the contract that the enum already is, with its own length limit to keep in step.

## Decision

**A relational (EF Core) model stores an enum by its number.** The configuration of an enum property does not call `HasConversion<string>()`, `HasConversion<EnumToStringConverter<...>>()` or any converter to text, and has no `HasMaxLength` for it. A model that wants a smaller column says so with the type of the column (`HasConversion<short>()`), still a number.

A member of an enum has the number it was given, written in the enum (`Reseller = 2`), and a number is never reused or changed once data exists; a member that is not used any more stays, marked `[Obsolete]`. A test of a service reads its model and fails when an enum property has a converter to text. What a client sees is not the stored value: the DTO and the API keep their own contract (the `Display` key of the member is the text for a client, ADR-0145).

This is about relational stores. A document store (MongoDB) keeps what its serializer is told and is not covered by this decision.

## Consequences

Smaller rows and indexes and number compares; renaming a member costs nothing. A table read by hand shows numbers, so the numbers of an enum are written in it and a tool or a view that shows the name is left to whoever reads. A service that already stores names moves its enum columns to numbers in a new migration (or, before the first release, in a fresh `InitialCreate`).

## Related

[ADR-0145](0145-an-enum-has-a-key-and-a-name-through-the-display-attribute.md), [docs/EntityConcepts.md](../EntityConcepts.md), the rule [data-persistence](../../.claude/rules/data-persistence.md).
