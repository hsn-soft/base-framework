# Hsx

A copy of the *shape* of the hhs system, built on the new framework ([ADR-0058](../docs/adr/0058-hsx-imitates-hhs-to-test-the-new-framework.md)): `service/identity` (tenants as a tree, users, roles, companies, customers, subscriptions), `service/content` (the content of a customer's subscription), `app/Hsx.AuthServer.Host`,
`gateway/Hsx.Gateway.Commercial.Host`, `app/Hsx.CommercialWeb.Host` (Razor Pages) and `shared/`.

## Run it

PostgreSQL and Redis of the local infrastructure on their default ports. Make the keys of the tokens first:

```
dotnet run samples/scripts/generate-rsa-keys.cs -- samples/hsx/app/Hsx.AuthServer.Host/private_key.xml samples/hsx/shared/Hsx.Shared.Hosting/public_key.xml
samples/hsx/run-all.sh        # the identity and content services make their databases (Hsx_Identity_Dev, Hsx_Content_Dev) and the demo data
samples/hsx/smoke-test.sh     # what each demo user sees through the gateway (7200)
```

The web app is `http://localhost:7300`. Demo users (password `Hsx-Demo-123`): `system-admin@hsx.local`, `admin@techsummus.hsx.local`, `admin@pixel.hsx.local`, `admin@acme.hsx.local`. The home page shows what the token of the signed-in user lists (`allowed_tenant_id`, `allowed_scope_key`).

Endpoints: `POST /{identity-service|content-service}/v1/{tenants|app-users|app-roles|companies|customers|subscriptions|customer-contents|analysis-contents|analysis-content-items|customer-content-visits|customer-vp-settings}/{get/{id}|paged-list|create}`, `PUT` (update) and `DELETE /{id}`, through the gateway.
