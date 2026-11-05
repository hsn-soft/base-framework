# ADR-0064: Who owns a record in Hsx (the tenant, the scope or neither), who writes the sales, and how a sale moves from one reseller to another

- **Status:** Accepted
- **Date:** 2026-11-03 (supersedes [ADR-0063](0063-companies-customers-and-subscriptions-belong-to-the-tenant-that-made-them.md); applies [framework ADR-0128](../../../docs/adr/0128-the-tenants-that-a-tenant-sees-are-the-ones-under-it-by-a-path-of-ids.md) and [ADR-0129](../../../docs/adr/0129-an-entity-is-isolated-by-its-tenant-or-by-its-scope-never-both.md); the model and the checklist for a new entity are in [TenancyModel.md](../../hsx/docs/TenancyModel.md))

## Context

ADR-0063 made a company, a customer and a subscription belong to the reseller that made them, after what the hhs report said a reseller does. The maintainer's model is another: the **companies and customers are defined in the system** (by the system tenant), a **reseller does not make them nor sell**, the system tenant records each sale for
a reseller, and a company or a customer is not a tenant, so it has no users or roles of its own (a user and a role always need a tenant). Two resellers cannot sell the **same product** to a customer, but can sell **different** products to it (audio-time sells the video platform to the customer `cnbce` of the company `ilbak`, tech-summus
the podcast, epartner the e-invoice). A sale can pass from one reseller to another.

## Decision

- **Who is who.** The tenants are the **system tenant** and the **resellers** (`tech-summus`, `audio-time`, `epartner`). A **company** and a **customer** are a global catalogue: no tenant, no filter. Users and roles belong to a tenant (a reseller makes the users and roles of the people of a company or a customer it sold to: they are its own).
- **The system tenant writes** the companies, the customers and the subscriptions (a sale, its change, its end, its transfer). A reseller does not make them; it **sees its own subscriptions**, and the companies and customers that are in them are what it filters by (`subscriptions/filters`); it has no endpoint to the catalogue.
- **A subscription is `IMultiTenant`** (the global filter of the tenant; `TenantId` is the reseller that sells it). The places that read across tenants say so (the sign-in, `me/subscriptions`, the check of the uniqueness). **A customer has one active subscription for a product** (unique on customer and product while it is not ended or deleted); its **scope key
  names the customer and the product, never the reseller**.
- **A role has a type**, and the type says which subscriptions it sees: **System** (the roles of the system tenant: everything, no links), **Tenant** (every subscription of the tenant), **Company** (the subscriptions of the tenant for a company: the role names the company) and **Customer** (those for a customer: it names the customer). A
  user of a Company or Customer role sees the records of the products that tenant sold to that company or customer, and nothing else. The type and the company or customer a role names are **fixed when it is made** (another binding is another role). The **permissions** a role has are another thing: the type says which data, the permissions say which actions.
- **The links of a role to subscriptions (`AppRoleSubscription`) are kept by the system, not edited by hand**: a sale adds the subscription to the roles of its tenant that its type covers; the end of a sale takes it from every role; a new role gets the subscriptions that match it. They stay a table, so what a role sees can be read.
- **Who owns a new entity** follows the decision of [TenancyModel.md](../../hsx/docs/TenancyModel.md): data **about what a customer does with a product** that goes on whoever sells it (content, settings, requests, videos, visits) is isolated by the **scope key**; a **business record of the reseller** (statistics for billing, payments, invoices, commissions) is isolated by the
  **tenant, stamped when the record is made with the reseller of that time and never changed**; what is a tenant's own (users, roles, policies) is the tenant's; the catalogue and the system's own data have neither. Never both on one entity (ADR-0129): an entity that seems to need both is two entities.
- **A transfer** (`subscriptions/{id}/transfer`, the system tenant only) ends the old subscription and starts a new one for the new reseller in **one transaction**: the old row gets `EndedAt` (it stays in the history of the old reseller), the new row starts now with the same product settings, the links of the old subscription are taken from every role, and the
  refresh tokens of the users those roles had are revoked (their access tokens end by themselves). Because the scope key does not change, **what is isolated by the scope (the content and what follows it) stays with the customer and the product and the new reseller sees all of it, from before the transfer too**; **the business records
  of the old reseller stay its own** (they are stamped with it), and it goes on seeing them and the ended subscription; the new reseller sees the records made after the transfer.
- **The token** does not limit the scope keys by default (`Auth:MaxScopeKeysInToken`, 0 is no limit; the allowed tenants stay at 50): the keys of all the roles of a user are in it, and the headers of a request are 32 KB, so some six hundred keys is where a request is refused; a limit or a selection of keys is decided when a user comes near it.

## Consequences

- ADR-0063 is superseded: `Company` and `Customer` lose their tenant (and the account tenant of a company is not made: a company has no tenant), the `tenant-management` calls of a reseller (a company, a customer, a sale) are gone, and the system tenant makes them with the ordinary endpoints; only the making of a reseller stays.
- A reseller sees nothing of another reseller's sales, and cannot learn that a customer exists unless it sold to it.
- The event that tells the other services of a transfer is not published yet: no service handles it, and the identity service has no broker (it comes with the first service that needs it).
- The rows of the ended subscriptions stay: a statistic or a payment of the old period names its subscription, and the reports that run across a transfer join by customer, product and period.
