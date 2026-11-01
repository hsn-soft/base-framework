# ADR-0130: The packages are pushed to NuGet with trusted publishing, not with an API key

- **Status:** Accepted
- **Date:** 2026-11-01 (changes the push step of [ADR-0115](0115-versions-and-releases-two-branches-and-a-version-made-from-nuget.md))

## Context

NuGet.org announced that API keys with a duration longer than 30 days expire on **1 November 2026**, that new keys are limited to 30 days from 17 August 2026, and that GitHub Actions should use **trusted publishing**. Our release workflow pushed with a long-lived key kept in the secret `GLOBAL_NUGET_TOKEN`: the next push after
the expiry would fail, and a key of 30 days would have to be renewed by hand every month.

## Decision

- `deploy-global-nuget.yml` no longer has an API key. The job has `id-token: write`; the step **`NuGet/login@v1`** (its `action.yml` says `node24`, [ADR-0111](0111-the-actions-of-the-workflows-run-on-node-24.md)) exchanges the OIDC token of the run for an API key that works for an hour, and `dotnet nuget push`
  uses that key (`steps.nuget-login.outputs.NUGET_API_KEY`). The login is the step just before the push, after the build and the tests, so the hour starts when it is needed.
- The one secret that stays is **`NUGET_USER`**, the **user name of the NuGet.org profile that owns the packages** (not an email address, and not a secret in the sense of a credential: it only says whose policy to look at).
- On nuget.org, the owner of the packages makes a **trusted publishing policy** (Account → Trusted Publishing): repository owner `hsn-soft`, repository `base-framework`, workflow file `deploy-global-nuget.yml`, and no environment (the workflow has none). A policy for a package owner that has no package of that
  name yet is a pending one and becomes active with the first push; the owner of existing packages makes it for the profile.
- After the first successful release with it, the secret `GLOBAL_NUGET_TOKEN` is **deleted** from the repository and the old API key is deleted on nuget.org (it expires on its own, but a key nobody uses is one that can leak).

## Consequences

- No secret has to be renewed: a key lives for the run. A fork or another workflow cannot push, because the policy names this repository and this workflow file; renaming the file means changing the policy first.
- The release cannot be tried without nuget.org: the first run on `rel/nuget-candidate` after the policy is made is the test (a candidate is a pre-release, so a failure costs nothing).

## Related

[ADR-0115](0115-versions-and-releases-two-branches-and-a-version-made-from-nuget.md), [ADR-0111](0111-the-actions-of-the-workflows-run-on-node-24.md), [ADR-0007](0007-build-settings-and-versioning.md).
