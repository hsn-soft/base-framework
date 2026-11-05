# ADR-0150: A normalized field is never empty, is unique in its set, and is checked before the database

- **Status:** Accepted
- **Date:** 2026-11-05

## Context

An entity keeps next to a typed text its `Normalized...` twin (`StringHelper.Normalize`, ADR-0149), and the services compare, search and make unique by the twin. Three things were loose in the services built on the framework: a text with nothing left after normalizing (`" 日本 "`) was stored as an empty twin that every other such text collides with; the unique sets were decided one table at a time (the email of a user was unique in the whole system while its user name was unique in the tenant); and a duplicate that reached the database came back as a provider exception that no client can read. Queries also compared the typed text in places.

## Decision

- **A normalized field is never empty**: the entity refuses it where it is made or changed (`Guard.NotNullOrWhiteSpace` on the normalized text), the column is required, and a relational table has a check that it is not empty (`CK_<Entity>_<Field>_NotEmpty`). A twin of an optional text is empty only when the text is not given (it is then null, not empty).
- **A normalized field is unique in the set the service identifies the row by**: a catalogue row by its name alone, a row that belongs to a tenant or a parent by the tenant (or the parent) and the name. The set is written in the model as a unique index and is the same set the service looks for a duplicate in.
- **Every create and update that can break a set asks the repository first** (with the filters off, since a deleted row keeps its text) and throws the business `...DuplicateException` of the entity; the index is the last guard and is never what a client sees.
- **A query matches an `Id` or a normalized field**, never the typed text. The application layer normalizes what came in (`ŞaSaN` is `SASAN`) and the repository only compares the normalized column: a repository method that takes a text takes a normalized one and its parameter says so (`normalizedEmail`).
- A test reads the model and lists the unique sets and the checks, so a new normalized field is a new line in it.

## Consequences

An empty normalized text cannot be stored by any path; a duplicate is a readable business error; a lookup by a typed text must normalize it first. A service that already has these fields adds the checks in a new migration (before the first release, a fresh `InitialCreate`). A document store has no check: the entity guard and the unique index are its guards.

## Related

[ADR-0149](0149-normalize-changes-the-turkish-letters-first-and-keeps-only-capitals-digits-and-a-few-marks.md), [ADR-0148](0148-an-enum-is-stored-by-its-number-in-a-relational-database-never-by-its-name.md), [docs/EntityConcepts.md](../EntityConcepts.md), the rule [data-persistence](../../.claude/rules/data-persistence.md).
