# ADR-0103: The brokers are reached with TLS and SASL when the settings say so, through one place per broker

- **Status:** Accepted
- **Date:** 2026-10-28 (completes the connection settings of [ADR-0023](0023-event-bus.md))

## Context

Neither broker could be reached securely: Kafka had only `BootstrapServers` (every client was plaintext) and RabbitMQ had no TLS option and a default login of `guest`/`guest`. A service in a cluster that is not a closed network, or any cloud broker, needs both. The clients were also made in five places on
Kafka (producer, consumer, two admin clients, the dead-letter producer) and in two on RabbitMQ (the persistent connection and the health check), so a setting added to one would have been missing from the others.

## Decision

- **Kafka (`KafkaOptions`, section `Kafka`):** `SecurityProtocol` (`Plaintext` by default), `SaslMechanism`, `SaslUsername`, `SaslPassword`, `SslCaLocation`, `SslCertificateLocation`, `SslKeyLocation`, `SslKeyPassword`. **Every client is configured by `KafkaOptions.Apply`**, so none can be left out.
- **RabbitMQ (`RabbitMqConnectionSettings`, section `RabbitMq:Connection`):** `UseTls` (TLS 1.2 or 1.3, the certificate of the broker is checked against `TlsServerName`, which defaults to `HostName`), `ClientCertificatePath` and `ClientCertificatePassword` (mutual TLS). **Both connections are made by `RabbitMqConnectionFactory`**; the health check's connection has no automatic recovery (it is opened to be closed).
- **Defaults do not change** (plaintext, port 5672): a service that works keeps working; a secure one sets the options. The framework does not refuse a plaintext broker, which a closed network may legitimately use.
- **The default login is warned about:** a connection with `guest`/`guest` to a host that is not local logs `RABBITMQ_CONNECTION_DEFAULT_CREDENTIALS` (Warning). No credential is ever logged.
- **Secrets stay out of files:** the passwords are settings that come from the environment or a secret store; the options say so in their comments ([CLAUDE.md](../../CLAUDE.md) says the same of the repository).

## Consequences

- A product can reach SASL/SSL Kafka and TLS RabbitMQ (and mutual TLS) with settings alone.
- Tests are on the settings reaching every client (unit); a broker with TLS is **not** tested in an integration test (it needs certificates in the test container): the encrypted connection itself is the client library's.
- Not done: SASL with OAUTHBEARER or Kerberos (needs callbacks), client certificates in a store instead of a file, and a refusal of plaintext outside a local host.

## Related

[ADR-0023](0023-event-bus.md), [ADR-0099](0099-rabbitmq-publishes-with-confirms-and-a-timeout.md).
