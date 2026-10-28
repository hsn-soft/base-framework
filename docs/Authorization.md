# Authorization

`HsnSoft.Base.Authorization` lets an endpoint ask for a **permission** and the code ask what the caller may do. Why it is this way: [ADR-0081](adr/0081-permissions-a-package-grants-in-the-cache-and-one-service-that-manages-them.md), [ADR-0083](adr/0083-permission-types-the-menu-and-constraints-that-must-be-given.md).

## In a service

```csharp
builder.Services
    .AddBaseRedis(configuration)                       // an IDistributedCache: where the grants are read from
    .AddBaseJwtAuthentication(configuration)           // ICurrentUser: the user and its roles
    .AddBasePermissions(configuration)                 // the attribute, the handler, the checker, the store
    .AddPermissionScope<TenantPermissionScope>();      // only when the product has scopes (the tenant)
```

```csharp
[PermissionAuthorize(EdxEndpointPermissions.Invoice.Invoices.Read)]    // 401 when not signed in, 403 when not granted
[HttpPost("paged-list")]
public Task<PagedDataResultDto<InvoiceDto>> GetPagedListAsync(...) => ...;
```

```csharp
// inside an action: a constraint that must be given (403 before anything is read when it is not), and an operation
decimal limit = await permissions.RequireDecimalAsync(EdxConstraintPermissions.Invoice.MaxViewAmount);
bool price = await permissions.IsGrantedAsync(EdxOperationPermissions.Invoice.PriceView);
```

gRPC takes the attribute on a method as a controller does (`PermissionDenied` for a refusal); a WCF operation is not an endpoint of the pipeline, so it asks `IPermissionChecker` itself.

## The permissions

A permission is a code, `{type}.{resource}.{action}` (three parts at least), in small letters, digits and dashes, and is a public string constant of a class nested by resource; the constants are the only list (`PermissionCatalog.CodesOf(typeof(EdxEndpointPermissions))` reads them).
**The first part is the type** (`PermissionType`: `Page = 1`, `Endpoint = 2`, `Operation = 3`, `Constraint = 4`, in the `Permission` table; the entity derives it from the code).

| Type | Code | Asked by | What it decides |
|---|---|---|---|
| Page | `page.invoice.view` | the client (menu, route) | the item is in the menu and the page opens; the client only |
| Endpoint | `endpoint.invoice.read`, `endpoint.invoice.create` | `[PermissionAuthorize]` | the call is let in (403 otherwise): a page reads with one and has a button for each of the others |
| Operation | `operation.invoice.price.view` | `IPermissionChecker.IsGrantedAsync` | inside an action: a field is open or masked before it leaves the service |
| Constraint | `constraint.invoice.max-view-amount` | `IPermissionChecker.Require...Async` | inside an action: the limit or the filter of the business, **with the value the role was given; a caller with none is refused (403) before anything is done** |

The constraints of the framework: `GetValueAsync` (and `GetIntAsync`, `GetLongAsync`, `GetDecimalAsync`, `GetBooleanAsync`) answer `null` when there is none; `RequireValueAsync` (and `RequireIntAsync`, `RequireLongAsync`, `RequireDecimalAsync`) refuse.

## Who has what

A grant is for a **role** (by its name), or for a **user** (by its id; the framework supports it, the Edx sample does not build it), under a **scope** (the tenant: `IPermissionScopeProvider`, so a role of one tenant is not the role of the same name in another). The checker looks at the user first, then each role of
the token; any grant is a grant (there is no denial), and for a value the first one found is the answer.

Where the grants are kept: **`IPermissionStore`**, over `IDistributedCache` as JSON, one entry for each subject, key `{Permissions:KeyPrefix}:{scope}:{role|user}:{name or id}`, in small letters. A service reads and keeps what it read in memory for `Permissions:CacheSeconds` (30). The service that
manages the grants (Edx: the identity service) writes with `SetAsync`.

| Setting (section `Permissions`) | Default | Means |
|---|---|---|
| `KeyPrefix` | `permission` | the prefix of the keys, so the grants have a place of their own in the cache |
| `CacheSeconds` | 30 | how long a service keeps what it read (0: every time); the longest a change takes to be seen |

## The Edx identity service

It holds the catalogue (`Permission`: code, type, name, description, active), the grants of the roles (`RolePermission` for a page, an endpoint, an operation; `RolePermissionConstraint` for a constraint, with its value), the **menu** (`Menu`, `MenuPermissionMap`), the tenants, the users and
the roles, and writes the entries of the roles to the store when a grant changes, at start and every `Permissions:SyncMinutes` (5).

| Endpoint (`api/identity-service/v1/commercial/...`) | Permission | Does |
|---|---|---|
| `POST me/menus/get` | none (any signed-in user) | the menu of the caller as a tree, each page with what it needs and whether the caller has it |
| `POST me/permissions/get` | none | what the caller has: its roles' grants, with the values of the constraints |
| `POST users/paged-list`, `users/get/{id}` | `endpoint.users.read` | users (the email is masked without `operation.users.email.view`) |
| `POST users/create`, `PUT users`, `DELETE users/{id}` | `endpoint.users.create` / `.update` / `.delete` | make (with its roles), change, delete a user (not oneself) |
| `POST roles/filter-list` | `endpoint.roles.read` | roles |
| `POST roles/create`, `DELETE roles/{id}` | `endpoint.roles.create` / `.delete` | make a role, delete one no user has |
| `POST permissions/filter-list` | `endpoint.permissions.read` | the catalogue |
| `POST roles/{roleId}/permissions/get` | `endpoint.role-permissions.read` | what a role was granted |
| `PUT roles/{roleId}/permissions` | `endpoint.role-permissions.update` | replaces it: `{ "grants": [ { "code": "...", "value": "..." } ] }` (a constraint needs a value, any other permission takes none) |
| `POST tenants/filter-list` | `endpoint.tenants.read` | the tenants (a customer's user sees its own) |

A caller reaches the roles and users of its own tenant; a caller of the system tenant reaches every tenant's. The grants a new database starts with are in `DefaultPermissionGrants`; a permission added to the code is granted to the sample roles that name it when the identity service first sees it ([ADR-0082](adr/0082-a-new-permission-is-granted-to-the-sample-roles-when-it-is-added.md)).

### The menu

The menu is defined in the code (`MenuCatalog`) and kept in the tables at every start. An item has a **node type** (`Caption`: a label, only at the root; `Node`: opens and closes; `Leaf`: a page, at the root or under a node), a **channel** (`Mobile`, `BackOffice`) and a **dock** (`SidebarMenu`, `TopbarMenu`). The permissions of an
item are in the map, with what each is to it (`MenuPermissionRelationType`): `Visibility = 1` (the `page.` permission: the item is shown and the page opens), `Required = 2` (what the page reads with), `Action = 4` (an optional endpoint, a button), `Operation = 5` (a field of the page), `Constraint = 6` (a limit).
**Every leaf has a visibility permission**, or the service does not start. A leaf is in the menu of a caller only when the caller has its visibility permission; a node when something under it is; a caption when something after it, up to the next caption, is.

## Every kind at work: the Edx invoices and invoice reports

`InvoicesController` / `InvoiceAppService` and `InvoiceReportsController` / `InvoiceReportAppService` of the invoice service (and the controllers of the intake service) are the example (MongoDB, one tenant's data each). Invoices are not made by an endpoint of the invoice service: they are sent through the intake service and come out of its queue as drafts
([ADR-0085](adr/0085-the-edx-invoice-pipeline-queue-import-draft-approve-sign.md)); a person approves or rejects them.

| Where | Permission | What it decides |
|---|---|---|
| menu | `page.invoice.view`, `page.invoice-draft.view`, `page.invoice-create.view`, `page.invoice-upload.view`, `page.queue.view`, `page.invoice-report.view` | the item and the page |
| attribute of the action | `endpoint.intake.upload`, `.submit-model` (the API: REST, gRPC, WCF); `endpoint.intake-back-office.upload`, `.create` (the pages); `endpoint.queue.read`, `.download` | whether the role may send an invoice and read its queue (403) |
| | `endpoint.invoice.read`, `.approve`, `.reject`, `.xml`; `endpoint.invoice-report.read`, `.generate`, `.delete` | whether the role may call the action (403) |
| when the answer is built | operation `operation.invoice.price.view` | the prices and the total are in the answer, or `null` |
| | operation `operation.invoice.tax-id.view` | the tax ids are in the answer, or left out |
| | operation `operation.invoice-report.total.view` | what a report amounts to, or `null` |
| inside `GetXml` | the two operations above | the XML holds both, so a role without one is refused (403) |
| inside the reads | constraint `constraint.invoice.max-view-amount` | the list holds the invoices up to it, an invoice above it is not found; no constraint, 403 |
| inside the upload | constraint `constraint.intake.upload.max-bytes` | the zip may not weigh more (400, `upload_too_large`); no constraint, 403 |
| inside the import | constraint `constraint.invoice.max-create-amount` | checked on the draft **as the sender** (the event carries who sent it): over it the invoice fails at the stage `Limit` |
| inside `Approve` | constraint `constraint.invoice.max-approve-amount` | the invoice may not amount to more (400, `invoice_total_over_limit`); no constraint, 403 |
| inside the reports | constraint `constraint.invoice-report.max-days` | the reports of the last days only, and a period no longer than that (400, `invoice_report_period_over_limit`) |

Every tenant (the system tenant too) has the four roles `admin`, `manager`, `operator` and `user`, with a user of each name; the seeder adds what is missing at every start (`SampleIdentity`). What they start with (`DefaultPermissionGrants`): the **admin** does all of it, up to the largest value; the **manager** sees (up to 100 000), reads the queue, approves or rejects
drafts (up to 50 000) and sees the people; the **operator** makes invoices (the create page, the upload page, the API; up to 1 000), reads the queue and the invoices up to 1 000 and sees prices but no tax id; the **user** only reads.
`samples/edx/smoke-test.sh` runs all of this, and the back office ([ADR-0084](adr/0084-the-edx-back-office-an-angular-app-that-asks-the-services-what-the-user-may-do.md)) shows it.
