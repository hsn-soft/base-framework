# How it works: the HsnSoft Base Framework

The framework is the infrastructure code a company's services repeat, written once and independent of any product.

Read this first; the details are in [Overview](Overview.md). Why each part is as it is: the [ADRs](adr/README.md).

## The system in short (HHS)

HHS puts a video player on the sites of publishers, the **customers**. A customer adds one script line to its pages (the loader, served by `hhs-app-commercial`); the loader loads the player and the SDK, and the SDK asks the **commercial gateway** (`hhs-gateway-commercial`) for the video of the page. The **content service** (`hhs-service-content`) makes the content when the page is new; the **text normalizer** (`hhs-service-text-normalizer`) scrapes the page and has an outline provider write its outline; the content service decides whether a video is made; the **video generator** (`hhs-service-video-generator`) makes the audio and the video with providers and tells the content service when it is `READY`. The next visit of the page gets the video URL (an analysis video meanwhile). The customer puts ads in front of the video, and the player reports what happened to each ad to the content service. The **feedr service** (`hhs-service-feedr`) reads the earnings from Google Ad Manager into a report pool that the customers see in the **back office** (`hhs-app-back-office`, through `hhs-gateway-back-office`); the back office signs in at the **auth server** (`hhs-app-auth-server`) with the users of the **identity service** (`hhs-service-identity`). The **scheduler** (`hhs-app-scheduler`) calls the timed jobs of the services, the **event manager** (`hhs-service-event-manager`) keeps the failed events for a manual requeue, and the **notification service** (`hhs-service-notification`) sends emails.

The system is multi-tenant, and a customer is bound to a **subscription**, not to a tenant: one tenant can sell the video platform for a site and another tenant the podcast product for the same site. Everything builds on `hhs-shared` (the shared packages) and on the HsnSoft Base Framework (`base-framework`).

## What it does here

It is a repository of **small packages, one capability each** (logging, hosting and the web api shell, authentication, multi-tenancy, caching, the event bus, the database providers, and so on), published to **NuGet** as a public library. A product takes only the packages for the technologies it uses: one service may use Kafka and another RabbitMQ, one PostgreSQL, another MongoDB, or both, and a host of several chooses which one prepares the database. Nothing forces a broker, a database or a cache on a service. The packages and what each depends on are in [Overview](Overview.md).

## How it is used and released

- A repository of the system (`hhs-shared`, a service, a gateway, an app) takes the packages it uses from NuGet at **one version** (the property `HsnSoftFrameworkVersion`).
- A change is tried first with **local builds** in `hhs-shared` and the services (`scripts/use-local-packages.sh` there), then the maintainer publishes a **candidate** (a release candidate) and later a **stable** version; the branches and the scripts are in [Releasing](Releasing.md).
- The `samples/` (`abc`, `edx`, `hsx`) are products written on the framework as if it did not know them; `hsx` is the system of HHS rebuilt on it, to test the framework.
- Every rule is an ADR, and the rules Claude has to keep are in `.claude/rules` (see `CLAUDE.md`).

## What is fixed and what is a choice of the product

The capabilities, their options and their defaults are the framework's; **which technology a product uses, its settings and its secrets are the product's**.

## Locally

`dotnet build`, the tests, and the detector (`./scripts/ci-detector/ci_detector_mac.sh`) before a pull request ([Checks](Checks.md)).
