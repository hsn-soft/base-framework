# Edx back office

An Angular 19 application (standalone components, signals, lazy routes, plain CSS) for the people of the Edx system: the invoices, the invoice reports, and the users and roles with what each role is granted. It talks only to the gateway (`src/app/core/config.ts`, `http://localhost:5600`).
Why it is built this way: [ADR-0084](../../../../docs/adr/0084-the-edx-back-office-an-angular-app-that-asks-the-services-what-the-user-may-do.md); the permissions it shows: [docs/Authorization.md](../../../../docs/Authorization.md).

```
npm install
npm start          # http://localhost:4200  (samples/edx/run-all.sh starts it with the hosts)
npm run build
```

Sign in with the tenant code, the user name and the password of a sample user (the identity service's `appsettings.development.json`): `veriban` / `operator`, `bilgera` / `manager`, `system-tenant` / `admin`, ...

| Folder | What |
|---|---|
| `src/app/core` | the calls (`api.ts`), signing in (`auth.service.ts`), the **session** (the menu and the permissions of the user, `session.service.ts`), the guards (`guards.ts`) and the interceptor |
| `src/app/layout` | the shell: the menu on the left is the one the identity service gave this user, Home is the only item of its own |
| `src/app/pages` | home (the welcome and the warning for a user with no page), forbidden, login, invoices, invoice reports, users, roles |

A page that is a menu item is opened by `menuGuard` only when the user's menu has it (its visibility permission); a button is enabled when its permission is among the user's grants; the services check every call again.
