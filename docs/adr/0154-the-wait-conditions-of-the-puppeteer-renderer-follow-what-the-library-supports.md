# ADR-0154: The wait conditions of the Puppeteer renderer follow what the library supports

- **Status:** Accepted
- **Date:** 2026-11-05 (adds to [ADR-0141](0141-a-rendered-page-is-an-untrusted-caller-and-the-browser-is-a-pool-with-limits.md))

## Context

`ChromeRenderingTests.EveryWaitCondition_GivesThePage` failed in the CI of a pull request into `dev`: the `NetworkIdle` and `DomContentLoaded` cases of an HTML request each waited the whole 40 s of the call (`TimeoutException`), the `Load` case passed in 0.8 s, and the test passed every time on a developer machine (six runs in a row). The cause is in the library: PuppeteerSharp documents that `Networkidle0` and `Networkidle2` are **not supported
for `SetContent`**, are ignored, and "have never worked reliably for this operation in upstream Puppeteer"; its advice is to use `WaitForNetworkIdleAsync` after setting the content. The renderer passed the condition on to `SetContent` as it was.

## Decision

- **For an HTML request the renderer calls `SetContentAsync` with `Load` always**, and waits for `NetworkIdle` apart with `page.WaitForNetworkIdleAsync` (the timeout of the navigation). `load` comes after `DOMContentLoaded`, so `Load` stands for `DomContentLoaded` too; the wait for the lifecycle event of `DOMContentLoaded` by itself is not used for HTML (it hung together with the network idle in that run; the library says nothing of it, so
  that part is a precaution, not a proven cause).
- **For an address the condition goes to `GoToAsync` unchanged** (navigations support all of them).
- **A wait for something the library documents as unsupported is not written**: before a `WaitUntil`, `SetContent` or any other call of PuppeteerSharp is given a condition, its XML documentation (`~/.nuget/packages/puppeteersharp/<version>/lib/net10.0/PuppeteerSharp.xml`) is read for what it does not support.

## Consequences

- A request with HTML and `NetworkIdle` waits for the network to be idle (500 ms with no connection) after the load, and not for ever.
- The failure could not be reproduced locally (it is of a slow runner); the proof is the CI of the pull request. A test of the Chrome that needs a condition to be met follows [the rule of slow CI](../../.claude/rules/testing-ci.md) (a bounded wait, no assumption of speed).

## Related

[ADR-0141](0141-a-rendered-page-is-an-untrusted-caller-and-the-browser-is-a-pool-with-limits.md), [the Browser guide](../Browser.md).
