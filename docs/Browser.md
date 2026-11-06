# A browser as a service: `HsnSoft.Base.Browser`

An application that needs the page after its scripts ran (a scraper), a PDF (an invoice, a report) or an image of a page (a preview) asks `IBrowserRenderer`; Chrome does it behind the contract. Why it is this way: [ADR-0141](adr/0141-a-rendered-page-is-an-untrusted-caller-and-the-browser-is-a-pool-with-limits.md).

| Package | What |
|---|---|
| `HsnSoft.Base.Browser` | The contract (`IBrowserRenderer`, `BrowserRequest`, `RenderedPage`, `PdfOptions`, `ScreenshotOptions`), the exceptions and `BrowserUrlPolicy`; references `Extensions` only |
| `HsnSoft.Base.Browser.Puppeteer` | The Chrome implementation (PuppeteerSharp), the settings, the readiness check |

```csharp
builder.Services.AddPuppeteerBrowser(builder.Configuration);        // section Browser; ExecutablePath is required

byte[] pdf = await renderer.RenderPdfAsync(BrowserRequest.FromHtml(html), new PdfOptions { Format = PaperFormat.A4 }, cancellationToken);
RenderedPage page = await renderer.GetPageAsync(BrowserRequest.FromUrl(new Uri(address)) with { WaitUntil = WaitCondition.NetworkIdle, WaitForSelector = "#content" }, cancellationToken);
byte[] png = await renderer.ScreenshotAsync(BrowserRequest.FromUrl(new Uri(address)), new ScreenshotOptions { FullPage = true }, cancellationToken);
```

A request has an address (http or https) **or** HTML, not both, and may set the wait condition (`Load`, `DomContentLoaded`, `NetworkIdle`), a selector to wait for, a timeout (1 to 600 seconds), the viewport, a user agent and headers. **With HTML, the wait condition is met in two steps** ([ADR-0154](adr/0154-the-wait-conditions-of-the-puppeteer-renderer-follow-what-the-library-supports.md)): `SetContent` waits for `load` (which comes after `DOMContentLoaded`, so it stands for `Load` and `DomContentLoaded`), and `NetworkIdle` is then waited for apart with `WaitForNetworkIdleAsync`, because PuppeteerSharp ignores the network-idle values in `SetContent` and the wait hung until the timeout of the call on a slow CI runner. With an address the condition goes to `GoToAsync` as it is. A page that cannot be rendered is a `BrowserException`, an address the policy refuses as the page itself is a `BrowserBlockedException`, a call that took longer than its time is a `TimeoutException`.

## The policy: what a page may load

A rendered page runs scripts and fetches what it names, so an address that came from outside can make the browser call the inside of the network (the cloud metadata address, a database, another service). **Every request of the page, the redirects and what its scripts fetch included, goes through `BrowserUrlPolicy`**, and the address of the page itself is checked before a page is opened:

- `http` and `https` to a host that is allowed and whose addresses are **all public** (the name is resolved by the policy; private, loopback, link-local, shared, documentation, multicast and reserved addresses, IPv4 and IPv6, are refused), and `data:` and `about:blank`, which load nothing;
- `file:`, `ftp:`, `chrome:`, `javascript:` and the rest are refused;
- `Browser:AllowedHosts` (a name or `*.example.com`) narrows it further; `Browser:AllowPrivateNetworks` is for a development machine.

A browser resolves a name again when it connects, so a name that changes its answer between the two lookups is not stopped by the policy: where that matters, the network (an egress rule) must not let the browser reach the inside.

## Settings (`Browser`)

| Setting | Default | Meaning |
|---|---|---|
| `ExecutablePath` | required | The Chrome or Chromium to start. **It is never downloaded by the application** |
| `DisableSandbox` | `false` | `--no-sandbox`. Chrome refuses to start as root with its sandbox; a container that runs as root has to turn it on, and then the page it renders can reach the machine: run the container as a user that is not root where you can |
| `DisableDevShmUsage` | `false` | For a container whose `/dev/shm` is small (Docker gives 64 MB) |
| `IgnoreCertificateErrors` | `false` | Development only |
| `ProxyServer`, `ExtraArguments` | none | A proxy for every request; more arguments for Chrome |
| `MaxConcurrentPages` | 4 | The others wait |
| `WaitForPageSeconds` | 30 | How long a call waits for a place before it is a `TimeoutException` |
| `OperationTimeoutSeconds` | 60 | The time of a whole call (the request can set its own) |
| `NavigationTimeoutSeconds` | 30 | The time a page may take to load |
| `RecycleAfterPages` | 200 | After this many pages Chrome is replaced by a new one (it grows); 0 for never |
| `LaunchAttempts` | 2 | Tries to start Chrome |
| `ShutdownTimeoutSeconds` | 20 | A stopping host waits this long for the pages being rendered, then ends them |
| `AllowedHosts`, `AllowPrivateNetworks` | any public host, `false` | The policy |

Chrome is started when the first page is needed, not when the host starts, and again when it is gone. The readiness check (`browser`) is ready when the Chrome of `ExecutablePath` is there; it does not start a browser for a probe.

## In an image

Install a Chrome in the image and run the container as a user that is not root, so the sandbox can stay on (a Debian image: `chromium`, `fonts-liberation` and the fonts of your languages). Set `Browser:ExecutablePath` to it, and `DisableDevShmUsage` unless `/dev/shm` is big.
