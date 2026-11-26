# ADR-0167: The `tlsCAFile` of a MongoDB connection string is applied by the framework, because the .NET driver ignores it

- **Status:** Accepted (extends [ADR-0119](0119-the-infrastructure-clients-share-their-connections-and-bound-their-waits.md): one more option of the connection string that `MongoOptions.Apply` handles)
- **Date:** 2026-11-26

## Context

A MongoDB that talks TLS with a certificate of a private CA (the CA of a cluster, as in the UAT of the services of this framework) is reached by the standard option `tlsCAFile=<path>` of the connection string. The .NET driver (`MongoDB.Driver` 3.14) reads the connection string but **does not apply** this option and ignores it without an error: it has only `tlsInsecure` and `sslVerifyCertificate`, and an unknown option is dropped. The first service that used it in UAT stopped with `UntrustedRoot` (the system does not trust the CA), although the file was in the pod and the chain was correct; the temporary answer was `tlsInsecure=true`, which turns off the whole check (the chain and the name), so a client could be sent to any server that talks TLS.

## Decision

- `MongoOptions.Apply` (used by `AddBaseMongo` for the default client and the keyed ones) reads `tlsCAFile` from the connection string (case-insensitive, decoded) and, when TLS is on, installs a certificate check on the connection (`SslSettings.ServerCertificateValidationCallback`, `MongoTlsCa.Validate`): the certificate of the server must chain to a CA of the file (a custom root of trust; the system store is not used for it) and its name must match the host. A missing certificate, another CA, an expired certificate or a name that does not match is refused. Revocation is not checked (a private CA has no list).
- The file (PEM, one or more certificates) is read **when the client is made**: a file that is not there or has no certificate is an error at the start of the service, not a refused connection later.
- `tlsInsecure=true` still turns the check off and wins over `tlsCAFile` (the file is then not read); a connection string without TLS or without the option is the driver's, as it was.

## Consequences

A service points at the CA with the standard option and verifies the server (chain and name): `tlsInsecure` is no longer needed for a private CA and the UAT workaround is removed with the new version. The framework owns the check, so a change of the driver that starts to apply the option must be read and this code removed then (the tests with a real TLS MongoDB show it). Only the CA of the server is handled: no client certificate (`tlsCertificateKeyFile`), which is a separate option.

## Related

[ADR-0119](0119-the-infrastructure-clients-share-their-connections-and-bound-their-waits.md); `MongoOptions`, `MongoTlsCa` (`src/HsnSoft.Base.MongoDB`); tests `MongoTlsCaTests` (unit) and `MongoTlsCaFileTests` (real MongoDB with TLS, `MongoTlsFixture`).
