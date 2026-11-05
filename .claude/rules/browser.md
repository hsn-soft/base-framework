---
paths:
  - "src/HsnSoft.Base.Browser/**"
  - "src/HsnSoft.Base.Browser.Puppeteer/**"
  - "test/HsnSoft.Base.Browser*/**"
  - "docs/Browser.md"
---

# Browser rendering (`HsnSoft.Base.Browser`, `HsnSoft.Base.Browser.Puppeteer`)

- A rendered page is an untrusted caller; the browser is a pool with limits (pages, time, a policy of what a page may load, private networks refused by default): keep every request of the page, redirects and scripts included, behind the policy (ADR-0141).
- **Read the XML documentation of PuppeteerSharp before giving it a condition or an option** (`~/.nuget/packages/puppeteersharp/<version>/lib/net10.0/PuppeteerSharp.xml`): some values are accepted and ignored. `SetContent` does not support `Networkidle0`/`Networkidle2` (ADR-0154); for HTML the renderer waits for `Load` and then calls `WaitForNetworkIdleAsync` when the request wants `NetworkIdle`; for an address the condition goes to `GoToAsync` as it is.
- A call that can wait has a bounded time (`OperationTimeoutSeconds`, the timeout of the request) and ends in a `TimeoutException`, never in a wait without end; a place in the pool is always given back.
- The tests of the real Chrome (`ChromeRenderingTests`) do nothing where no Chrome is installed (`HSN_TEST_CHROME`); the CI runner has one, so they run there: they must pass on a slow two-core machine (see `testing-ci.md`), and a failure that does not happen locally is read from the log of the run, not run again until it is green.
- Keep [docs/Browser.md](../../docs/Browser.md) and the settings table in step with the code.

Relevant decisions: ADR-0141, ADR-0154.
