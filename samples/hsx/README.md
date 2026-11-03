# Hsx

A copy of the *shape* of the hhs system, built on the new framework ([ADR-0058](../docs/adr/0058-hsx-imitates-hhs-to-test-the-new-framework.md)): `service/identity` (tenants as a tree, users, roles, companies, customers, subscriptions), `service/content` (the content of a customer's subscription), `service/text-normalizer` and `service/video-generator` (MongoDB, one entity and its CRUD each, [ADR-0059](../docs/adr/0059-hsx-has-two-mongodb-services-and-the-four-environments.md)), `app/Hsx.AuthServer.Host`,
`gateway/Hsx.Gateway.Commercial.Host`, `app/Hsx.CommercialWeb.Host` (Razor Pages) and `shared/`.

## Run it

PostgreSQL, MongoDB and Redis of the local infrastructure on their default ports. Make the keys of the tokens first:

```
dotnet run samples/scripts/generate-rsa-keys.cs -- samples/hsx/app/Hsx.AuthServer.Host/private_key.xml samples/hsx/shared/Hsx.Shared.Hosting/public_key.xml
samples/hsx/run-all.sh        # the identity and content services make their databases (Hsx_Identity_Dev, Hsx_Content_Dev) and the demo data
samples/hsx/smoke-test.sh     # what each demo user sees through the gateway (7200)
```

The web app is `http://localhost:7300`. Demo users (password `Hsx-Demo-123`): `system-admin@hsx.local`, `admin@tech-summus.hsx.local`, `admin@audio-time.hsx.local`, `admin@epartner.hsx.local`, `ilbak-admin@audio-time.hsx.local`, `cnbce-admin@audio-time.hsx.local`. The home page shows what the token of the signed-in user lists (`allowed_tenant_id`, `allowed_scope_key`). `system-operator@hsx.local` is the system tenant's person for the selling work (resellers, companies, customers, sales, demo contents; no deletes or grants).

**To try the tenant model end to end:** `samples/hsx/demo-scenario.sh [gateway]` makes a reseller, a company with two customers, the video platform sold to both, demo contents, and a Company and two Customer users with their roles, then prints what each of them sees. The same steps, one request at a time, are the Postman collection `docs/hsx.postman_collection.json` (import it, run the folders in order; change the `run` variable to run it again). A role made through the API starts with nothing granted, except a Company or Customer role, which starts with the read access to the data of its products.

## Tenants, companies, customers and subscriptions

[docs/TenancyModel.md](docs/TenancyModel.md) is the model and the checklist for a new entity ([ADR-0064](../docs/adr/0064-who-owns-a-record-in-hsx-tenant-scope-neither-and-how-a-sale-moves-between-resellers.md)): the system tenant writes the companies, the customers and the sales; a reseller sees its own sales; a role has a type (System, Tenant, Company, Customer); a sale passes from a reseller to another by ending and starting.

## Permissions and the menu

Every endpoint asks for a permission ([ADR-0062](../docs/adr/0062-the-permissions-and-the-menu-of-hsx-are-the-codes-and-live-in-the-identity-service.md)). The constants are in `shared/Hsx.Shared.Helper` (`HsxEndpointPermissions`, `HsxPagePermissions`); the menu and what each page needs are `MenuCatalog` of the identity service; the roles `SystemAdmin` and `Admin` start with `DefaultPermissionGrants`. `POST /identity-service/v1/me/menus` is the menu of the caller, `PUT /identity-service/v1/app-roles/{id}/permissions` sets what a role may do.

## Changing a schema (identity, content)

The PostgreSQL services use EF Core migrations, applied when the host starts ([framework ADR-0131](../../docs/adr/0131-a-schema-that-evolves-is-migrated-by-a-startup-step.md)). After a change of an entity or its configuration:

```
dotnet ef migrations add <Name> --project samples/hsx/service/content/Hsx.ContentService.EntityFrameworkCore --output-dir Migrations
```

The MongoDB services (text normalizer, video generator) have no migrations: a collection is made when it is first written and the indexes by a start step.

## Environments

`samples/hsx/run-all.sh` is development. `samples/hsx/deploy/deploy.sh docker|uat|production` builds the images and deploys the seven hosts (Docker Compose, or the Kubernetes of Docker Desktop in `hsx-uat` and `hsx-prod`, two pods of each); `undeploy.sh <environment>` takes it away and `test.sh uat|production` tries the health probes. The gateway and the web app are on 7200 7300 (development and docker), 7210 7310 (uat), 7220 7320 (production); the databases are `Hsx_*_Dev`, `_Uat`, `_Prod`. `smoke-test.sh http://localhost:7210` runs the same checks against an environment; add `read-only` to stop after what each demo user sees, so nothing is left in its databases.

Endpoints: `POST /{identity-service|content-service}/v1/{tenants|app-users|app-roles|companies|customers|subscriptions|customer-contents|analysis-contents|analysis-content-items|customer-content-visits|customer-content-vp-settings}/{get/{id}|paged-list|create}`, `PUT` (update) and `DELETE /{id}`, through the gateway.
The text normalizer and the video generator: `/text-normalizer-service/v1/customer-content-normalized-requests/...` and `/video-generator-service/v1/video-requests/...` with the same calls.
