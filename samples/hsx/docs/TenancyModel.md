# The tenancy model of Hsx

How the tenants, the companies, the customers, the subscriptions, the users and the roles of Hsx fit together, and what to decide when a new entity is added. The decisions and their reasons are [ADR-0064](../../docs/adr/0064-who-owns-a-record-in-hsx-tenant-scope-neither-and-how-a-sale-moves-between-resellers.md);
the isolation rules of the framework are [framework ADR-0128](../../../docs/adr/0128-the-tenants-that-a-tenant-sees-are-the-ones-under-it-by-a-path-of-ids.md), [ADR-0129](../../../docs/adr/0129-an-entity-is-isolated-by-its-tenant-or-by-its-scope-never-both.md) and [MultiTenancy.md](../../../docs/MultiTenancy.md).

## The parts

```
system-tenant ──────────────── makes ──► Company ◄── Customer      (a global catalogue: no tenant, no filter)
   │                                           │
   │ records each sale (and its change, end, transfer)
   ▼                                           ▼
Subscription  { TenantId = the reseller that sells, CustomerId, CompanyId, ProductType, ScopeKey = {customerId}:{product}, EndedAt }
   ▲  one active subscription for a (customer, product)
   │ links (kept by the system)
AppRoleSubscription ── AppRole { TenantId, RoleType, CompanyId?, CustomerId? } ── AppUserRole ── AppUser { TenantId }

tenants: system-tenant, then the resellers (tech-summus, audio-time, epartner); a company and a customer are not tenants
```

Example of the demo: audio-time sells the video platform to the customer `cnbce` of the company `ilbak`, tech-summus the podcast, epartner the e-invoice (three subscriptions, three scope keys). Audio-time makes a **Company** role bound to `ilbak` and a **Customer** role bound to `cnbce`, with a user
for each: the first sees the records of what audio-time sold to `ilbak`, the second those of what it sold to `cnbce`, and neither sees what tech-summus or epartner sold.

## Who may do what

| | system-tenant | a reseller |
|---|---|---|
| company, customer | makes, changes, deletes | no access (it filters by the ones in its subscriptions) |
| subscription | sells (for a reseller), changes, ends, transfers | sees its own |
| tenant (a reseller) | makes, changes, moves, deletes | sees its own and the ones under it |
| user, role | of any tenant | of its own tenant (and the tenants under it) |
| role links to subscriptions | kept by the system | read only |

## The role types

| Type | Whose | Sees | Named |
|---|---|---|---|
| **System** | the roles of the system tenant | everything (the system tenant is under no isolation; no links) | nothing |
| **Tenant** | a reseller | every subscription of the tenant | nothing |
| **Company** | a reseller | the subscriptions of the tenant for one company | the company |
| **Customer** | a reseller | the subscriptions of the tenant for one customer | the customer |

The type and what it names are fixed when the role is made. The **permissions** of a role (`page.*`, `endpoint.*`) are separate: the type says which **data**, the permissions say which **actions**. A role of a reseller can only name a company or a customer that is in the reseller's own
subscriptions.

**What keeps the links.** A sale adds its subscription to the roles of its tenant that the type covers (all Tenant roles, the Company roles of that company, the Customer roles of that customer); the end of a sale takes it from every role; a new role is given what matches it. Nobody edits a link by hand.

## What to decide for a new entity

Ask in this order; the first yes decides. **Never both a tenant and a scope on one entity** (ADR-0129): if two answers seem true, it is two entities.

1. **Is it data about what a customer does with a product, which goes on whoever sells it?** (content, settings of the customer, a request to make a video, a visit, the result of a job.) → **scope**: `IScopeSubscription`, `ScopeKey = {customerId}:{product}`, set by the code that makes it from the customer and the product, never from the caller. After a transfer the **old reseller no longer sees it and the new reseller sees all of it**,
   from before the transfer too. The service that makes it checks that the key is the caller's (`EnsureScope`), the events that carry it have the key in the body.
2. **Is it a business record of a reseller with a customer?** (statistics for billing, a payment, an invoice, a commission, a report row.) → **tenant**: `IMultiTenant`, with `TenantId` **stamped when the record is made with the reseller that sells at that time, and never rewritten**; plus plain columns for what it is about (`SubscriptionId`, `CustomerId`,
   `ProductType`, the period) to filter by. After a transfer the **old reseller keeps seeing what it made and the new reseller sees what is made after**; so the entity must carry the date it is about, and a report across a transfer joins by customer, product and period.
3. **Does it belong to a tenant by itself?** (a user, a role, a password policy, a grant.) → **tenant**: `IMultiTenant`, with the `TenantId` of that tenant (give it explicitly when it is made inside another tenant's scope).
4. **Is it the system's own, or the catalogue?** (a permission, a menu item, a tenant, a company, a customer.) → **neither**: no tenant, no filter; who may write it is the permissions' to decide (the system tenant only, for the catalogue).

### What to watch

- The tenant is stamped from the **current tenant** when the entity is saved if it has none: when the code works for another tenant (a seed, the system tenant acting for a reseller) it sets the tenant explicitly or runs in a scope of that tenant (`ICurrentTenant.Change`).
- A read that must cross tenants (the sign-in, `me/subscriptions`, a check of uniqueness) turns the filter off **on purpose and says why** in a comment; nothing else does.
- A **scope entity** has no tenant filter: a user is kept from it by the keys of its token, so anything that lists scope keys (the sign-in) must leave out a subscription that is ended, blocked or out of date.
- An event of a scope entity carries the scope key in its body; the tenant travels in the headers of the envelope. A handler of a scope event enters the scope of the key (`ICurrentTenant.Change(..., allowedScopeKeys: ...)`).
- Uniqueness that must hold across tenants (a customer and a product, a website, a name taken for ever) is checked with the filters off.
- A new service or page adds its permissions and its items to the menu in the same change (ADR-0062).

## A transfer, step by step (`subscriptions/{id}/transfer`, the system tenant only)

| What | What happens |
|---|---|
| the old subscription | gets `EndedAt`; stays, and stays visible to the old reseller as history |
| the new subscription | starts now for the new reseller: same customer, product and settings |
| the scope key | does not change (it names the customer and the product) |
| scope entities (content, settings, requests) | stay; the new reseller's roles see all of them; the old reseller's lose them |
| tenant entities (statistics, payments) | stay stamped with the old reseller, who keeps seeing them; new ones are stamped with the new reseller |
| role links | the old subscription is taken from every role; the new reseller's roles get the new one by their type |
| the sessions | the refresh tokens of the users of the roles that had the old subscription are revoked; their access tokens end by themselves |
