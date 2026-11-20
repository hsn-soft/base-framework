# ADR-0156: A browser request may run functions in the page, leave kinds of resources unloaded and hide that it is driven

- **Status:** Accepted (builds on [ADR-0141](0141-a-rendered-page-is-an-untrusted-caller-and-the-browser-is-a-pool-with-limits.md) and [ADR-0154](0154-the-wait-conditions-of-the-puppeteer-renderer-follow-what-the-library-supports.md))
- **Date:** 2026-11-10

## Context

`IBrowserRenderer.GetPageAsync` returned the HTML of the page after its scripts ran. A scraper that reads many sites needs three more things the previous system's browser did and a rendered HTML cannot: to run a small function in the live page (`innerText` of what is shown, the filter of a parent's class, a regular expression on a title) and get its result, to leave fonts, styles or scripts unloaded for a site whose pages load slowly with them, and to look like a person's Chrome for a site that answers a robot differently.

## Decision

- `BrowserRequest` gets `Scripts` (a name and the source of a JavaScript function with no argument, run in order after the page is ready), `BlockedResources` (flags: `Media`, `Fonts`, `Stylesheets`, `Scripts`, `Images`; the document of the navigation is never left out), `DisableCache` and `Stealth` (the properties a driven Chrome gives away are set as a person's Chrome has them, before the page's scripts run; it works for an address, not for HTML). All default to nothing: a request that does not ask is as before.
- `RenderedPage` gets `ScriptResults`: the result of each function as JSON, or the reason it failed (`ScriptResult`). **A function that fails does not fail the call**, nor the functions after it.
- The policy stays first: a request the policy refuses is refused whatever the flags say, and the flags only refuse more. A function runs in the page of the caller's request, so what runs is the caller's own code: a function is never taken from a customer's input without the caller's own check.
- `Browser:ProxyUserName` and `Browser:ProxyPassword` (secrets, from the environment) authenticate to `Browser:ProxyServer`.

## Consequences

An application reads a page by the function it names, as the previous system did, and keeps the policy, the pool and the limits of the renderer. The package is a new version of `HsnSoft.Base.Browser` and `Browser.Puppeteer`; callers that do not use the new members change nothing.

## Related

[ADR-0141](0141-a-rendered-page-is-an-untrusted-caller-and-the-browser-is-a-pool-with-limits.md), [ADR-0154](0154-the-wait-conditions-of-the-puppeteer-renderer-follow-what-the-library-supports.md).
