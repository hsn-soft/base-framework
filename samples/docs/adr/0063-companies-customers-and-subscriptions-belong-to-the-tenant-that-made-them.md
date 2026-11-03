# ADR-0063: Companies, customers and subscriptions belong to the tenant that made them, and the tenant management makes them

- **Status:** Accepted
- **Date:** 2026-11-03 (applies [ADR-0061](0061-administration-is-part-of-the-identity-service-and-the-auth-server-reads-its-database.md), [framework ADR-0128](../../../docs/adr/0128-the-tenants-that-a-tenant-sees-are-the-ones-under-it-by-a-path-of-ids.md) and [ADR-0129](../../../docs/adr/0129-an-entity-is-isolated-by-its-tenant-or-by-its-scope-never-both.md))

## Decision

- **`Company`, `Customer` and `Subscription` are `IMultiTenant`**: each belongs to the tenant that made it (for a subscription, the reseller that sold it: the column that was `ResellerTenantId` is `TenantId`), and a tenant sees its own and those of the tenants under it, by the query filter of the framework. hhs read
  the same visibility with a snapshot query of several hundred lines and checks written by hand in each service; here it is one filter on the entity, and it cannot be forgotten by a new endpoint.
- **A company has an account tenant of its own, explicitly** (`Company.AccountTenantId`): the tenant under the reseller where its people sign in. hhs gave the tenant the id of the company and matched them by that convention; the link is a column now.
- **The tenant management** (`tenant-management/...`) is what a reseller does to have customers, each call one transaction (`ITenantProvisioner`: the tenant, its `Admin` role with the starting grants, its first administrator; and the company for an account): the system tenant **makes a reseller**; a reseller **makes a company** (with
  its account tenant), **adds the customer** of a company by its website and **sells** a product to a customer. The last two are idempotent (the same company and website is one customer; the same reseller, customer and product is one subscription). A reseller sells and makes only as itself; the system tenant names the reseller.
  The names of a tenant, a company and a customer's website are taken for ever, a deleted one's too, and the email of the first administrator is the sign-in of the whole system, so all of them are checked before anything is written. The first administrator's password keeps the rules a new tenant starts with.
- **`me/subscriptions`** is the selector of the customers and products of a caller: the subscriptions whose scope key its token names (the system tenant: all), read with the tenant filter off, because the token, not the tenant, is what lets the caller see them.
- The sign-in reads the subscriptions of a user's roles without a tenant (nobody is signed in yet), as it reads the user.

## Consequences

- **A second reseller cannot sell to a customer another reseller made**: it cannot see the customer, and the website is taken. hhs seeds that case (several resellers sell one customer) and the demo does too (Pixel sells the video product of a customer that Techsummus made), but only the system tenant sells on behalf of a
  reseller. How resellers share a customer in a running system is an open question for the maintainer, not decided here.
- The two products of one customer have two scope keys and the scope key does not name the reseller, so two resellers that sell the same product of a customer share its data, as in hhs.
- The checks of the tenant management leave tenants, companies and users behind (a deleted tenant keeps its name), so their names have a part that is new in every run, and the counts of the system tenant in the smoke test are "at least"; `smoke-test.sh <gateway> read-only` makes none of them.
