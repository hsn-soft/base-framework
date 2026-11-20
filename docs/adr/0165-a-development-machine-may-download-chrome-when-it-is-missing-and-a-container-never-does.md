# ADR-0165: A development machine may download Chrome when it is missing, and a container never does

- **Status:** Accepted (refines [ADR-0141](0141-a-rendered-page-is-an-untrusted-caller-and-the-browser-is-a-pool-with-limits.md): the Chrome was never downloaded)
- **Date:** 2026-11-20

## Context

`Browser:ExecutablePath` was required and Chrome was never downloaded. A developer who clones a service on a new machine had to install Chrome and write its path (different on macOS, Windows and Linux) into a settings file or `.env`. The previous system downloaded Chromium when it did not run in a container, which was convenient but a risk in production (supply chain, start-up time, hosts with no internet).

## Decision

- `Browser:DownloadWhenMissing` (default `false`) lets a Chrome that is not at `ExecutablePath` (or a missing path) be downloaded once with the `BrowserFetcher` of PuppeteerSharp, into `Browser:DownloadPath` (default `{LocalApplicationData}/HsnSoft/browser`, shared by the applications of the machine).
- **The renderer finds out itself whether it runs in a container** and never downloads there, whatever the flag says: `DOTNET_RUNNING_IN_CONTAINER`, `IS_IN_DOCKER_CONTAINER`, `KUBERNETES_SERVICE_HOST` or `/.dockerenv`. The flag is therefore safe to leave on in a shared settings file; the image has its Chrome.
- A Chrome that is there is always used first; the download happens at the first page, not at the start of the host, and once for the process.
- `ExecutablePath` stays required unless the flag is on. The readiness check is ready when the Chrome is there or would be downloaded.

## Consequences

A new machine runs with no setup: the services set the flag in their Development settings and no per-machine path is needed. A container or a pod without a Chrome fails with a clear message instead of reaching out to the internet. A machine that is not a container but must not download (production on a virtual machine) leaves the flag off, its default.

## Related

[ADR-0141](0141-a-rendered-page-is-an-untrusted-caller-and-the-browser-is-a-pool-with-limits.md).
