# ADR-0141: A rendered page is an untrusted caller, and the browser is a pool with limits (`Browser`, `Browser.Puppeteer`)

- **Status:** Accepted
- **Date:** 2026-11-03

## Context

The old `HsnSoft.Base.PuppeTeer` was used by exactly one hhs service (scraping in the text normalizer) and dropped with the old projects ([the analysis](../old-framework/4-web-tools.md)). Its design was good (a limit on pages, a lease for each page, a drain on shutdown) and its defaults were not: Chrome ran with `--no-sandbox` and `--ignore-certificate-errors`, it was downloaded when the application started, one static timeout of 30 minutes applied to everything, and nothing stopped a page from calling the inside of the network. A page that is rendered runs scripts and fetches what it names, so an address that came from outside is a caller that runs code in your network. The maintainer chose to have the capability in the framework: scraping, PDF and screenshot.

## Decision

- **Two packages, as every technology** ([ADR-0003](0003-abstractions-separate-from-providers.md)): `HsnSoft.Base.Browser` has the contract (`IBrowserRenderer`: the page after its scripts, a PDF, an image, from an address or from HTML) and `BrowserUrlPolicy`, with no browser in it; `HsnSoft.Base.Browser.Puppeteer` is the Chrome implementation on PuppeteerSharp.
- **A rendered page is an untrusted caller.** Every request of a page (the page, redirects, scripts, images, calls) is decided by `BrowserUrlPolicy`: `http` and `https` to an allowed host whose addresses are all public, and `data:` and `about:blank`; the address of the page itself is refused before a page is opened. The same address rules as the guard of the HTTP client ([ADR-0138](0138-a-call-to-another-service-has-a-correlation-id-safe-retries-and-a-token-that-goes-only-where-it-is-for.md)), which are in `Extensions` (`PublicAddress`, `HostPattern`) for both. What a browser resolves again at connect time is not stopped here; the network must not let it reach the inside where that matters.
- **The defaults are the safe ones**: the sandbox of Chrome is on (`DisableSandbox` is an explicit setting with the warning), certificates are checked, Chrome is **never downloaded** (`ExecutablePath` is required), and every call has a time (`OperationTimeoutSeconds`, `NavigationTimeoutSeconds`, a timeout on the request) and a place among `MaxConcurrentPages`.
- **The browser is a pool with limits**: started when the first page is needed and again when it is gone, replaced after `RecycleAfterPages` pages, a page is always closed; a stopping host refuses new calls, waits `ShutdownTimeoutSeconds` for the pages being rendered and then ends them and closes Chrome. The readiness check does not start a browser.

## Consequences

A service renders pages by one registration and one interface, and the dangerous settings are the ones it has to name. The cost: Chrome has to be in the image (the sandbox needs a user that is not root); a rendered page of a host the policy does not allow is aborted, so a page that loads from a private CDN needs `AllowedHosts` and `AllowPrivateNetworks` on purpose. The tests run against a real Chrome (`HSN_TEST_CHROME`, or the usual paths) and do nothing where there is none.

## Related

[ADR-0003](0003-abstractions-separate-from-providers.md), [ADR-0138](0138-a-call-to-another-service-has-a-correlation-id-safe-retries-and-a-token-that-goes-only-where-it-is-for.md), [Browser](../Browser.md), [the old framework analysis](../old-framework/README.md).
