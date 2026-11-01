---
name: add-provider
description: "Use when adding a new broker, database, cache, logger, blob store, or other technology-specific provider package to HsnSoft.Base."
---

# Add a provider package

## Procedure

1. Read `.claude/rules/architecture.md`, `.claude/rules/testing-ci.md`, `.claude/rules/dotnet.md`, the relevant domain rule, `CONTRIBUTING.md`, `docs/Overview.md`, and the feature reference for the capability. Read ADR-0003/0004/0070/0075 and the capability-specific ADRs.
2. Confirm the provider is a separate, opt-in package that depends on its provider-neutral abstraction. The abstraction must not reference the implementation. Keep alternative providers (for example Kafka and RabbitMQ) in sibling packages; do not make a broker/store aggregate that brings unused technologies to consumers.
3. Reuse shared capability behavior in the abstraction; keep only technology-specific transport, configuration, client lifetime, health, and integration details in the provider. Add dependencies only when implementation or public API needs them and obey the framework reference graph.
4. Create the provider project using `common.props`, package metadata/README, shared package versions, and the prescribed `.csproj` reference groups/order. Register the provider through the established extension-method pattern and document its configuration.
5. Add the matching unit-test project with focused behavior, failure, timeout, and boundary tests. Add real-service integration tests in `test/HsnSoft.Base.IntegrationTests`; use the existing Testcontainers fixture/lifecycle and verify the actual protocol, serialization, topology or persistence behavior rather than relying only on mocks.
6. Add both projects to `HsnSoft.Base.sln` manually in their matching solution folders; update the dependency row in `docs/Overview.md` and the relevant feature reference. Add an ADR and update `docs/adr/INDEX.md` if the provider creates a lasting architectural decision.
7. Run focused unit and real-service integration tests, then formatting, reference checks and the full CI detector. Do not treat coverage as a substitute for assertions or real-provider integration tests.

Do not publish, push, or create release branches as part of provider implementation unless the user explicitly requests that separate action.
