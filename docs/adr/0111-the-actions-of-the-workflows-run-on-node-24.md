# ADR-0111: The actions of the workflows are on the first major version that runs on Node.js 24

- **Status:** Accepted
- **Date:** 2026-10-30 (adds to [ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md))

## Context

Every job of the CI ended with the warning that Node.js 20 is deprecated and that the actions `checkout@v4`, `cache@v4`, `setup-dotnet@v4`, `upload-artifact@v4` and `download-artifact@v4` target it and are forced to run on Node.js 24. It did not break anything, but the forcing is a stopgap of the runners and the
actions had to move.

## Decision

- The workflows use the **first major version of each action whose `action.yml` says `node24`**, read from the repositories of the actions and not from memory: `actions/checkout@v5`, `actions/cache@v5`, `actions/setup-dotnet@v5`, `actions/upload-artifact@v6`, `actions/download-artifact@v7` (the earlier majors in between still say `node20`).
  The first Node 24 version changes the runtime and nothing else in how we use them; the later majors add things we do not use (checkout keeps its credentials in another file, upload-artifact can upload a file unzipped, download-artifact no longer unzips everything and fails on a hash mismatch).
- This applies to both workflows (`ci-control-workflow.yml` and `deploy-global-nuget.yml`). The runner needs version 2.327.1 or newer, which the runners of GitHub have; a self-hosted runner would have to be updated first.

## Consequences

- The warning of Node.js 20 is gone from the runs. The release workflow (`rel/nuget`) runs only on a release: its actions were upgraded together with the CI's and are first run there.
- Moving to a later major (when the first one is no longer supported) is a decision of its own, made by reading what that major changes.

## Related

[ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md), [ADR-0109](0109-the-ci-jobs-are-a-pipeline-built-once.md).
