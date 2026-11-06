# ADR-0050: Edx has the same environments, with a Kafka broker, MinIO buckets and an Angular build for each

- **Status:** Accepted
- **Date:** 2026-10-31 (applies [ADR-0049](0049-the-environments-of-the-samples-are-development-docker-uat-and-production.md) to Edx; the probes are the ones of framework ADR-0117, which links to `../../../docs/adr`)

## Context

Edx is the sample that uses Kafka, MinIO and an Angular application besides what Abc uses, and it has hosts that do not speak plain REST (a gRPC host, WCF hosts) and hosts that call each other by address (the mock center of the GİB and the GİB integration). Its environments (`development`, `docker`,
`uat`, `production`) follow [ADR-0049](0049-the-environments-of-the-samples-are-development-docker-uat-and-production.md): the infrastructure of this machine, told apart by what each environment owns.

## Decision

- **Databases and Redis** as in Abc: `Edx_<Name>_Dev|Uat|Prod`, and the prefix of the Redis keys `edx:`, `edx-uat:`, `edx-prod:`. SQL Server is reached as `sa` and the secrets (its password, the user of MinIO) are put in by `render-settings.sh` from the ignored `deploy/.env`, never committed.
- **Kafka has a broker for each environment**, since a broker has nothing like a database or a virtual host to tell environments apart: the default broker (`infra-kafka`) for `development` and `docker` (the containers of Compose join `local.infrastructure-network` and use the address the broker advertises inside it,
  `infra-kafka-node0:29092`), and `infra-kafka-uat` (`host.docker.internal:9192`) and `infra-kafka-prod` (`host.docker.internal:9292`) for the cluster. The two are in the compose file of the infrastructure (`base-infrastructure`). Telling environments apart by a prefix of the topics and the consumer groups on one broker is a decision of
  its own, left for later.
- **MinIO has a bucket for each environment**: the `Storage:Container` of a host gets `-uat` or `-prod` (`edx-invoice`, `edx-invoice-uat`, `edx-invoice-prod`).
- **The hosts that call each other do it by the names of the services of the environment**: the GİB integration reaches the mock center as `http://mock-center:8080/...`, the mock center reaches the WCF host of the GİB integration as `http://integration-gib-wcf:8080/EFatura/`, the gateway its services as `http://<service>:8080`.
- **The gRPC host has a second port, 8081, for HTTP/1** (`Kestrel:Endpoints:Health`): its port 8080 is HTTP/2 only, a probe of the platform speaks HTTP/1.
- **The Angular back office is built for each environment** (`ng build --configuration docker|uat|production`): `src/environments/environment.<name>.ts` replaces `environment.ts` and has the addresses of the gateway and of the two integration hosts (the browser is on this machine, so they are `localhost` ports);
  these three builds have optimization on, no source maps, hashed file names, and **stricter budgets** (initial 320 kB as a warning and 400 kB as an error, a script 300 kB / 500 kB, a component style 3 kB / 5 kB) than the 500 kB / 1 MB that were. Each build is an image of its own (`edx/back-office:<environment>`: nginx with the files of the build).
- **The samples seed demo users in every environment** (`Seed:Users` of the identity service) because the environments are local and one must be able to sign in; a real production seeds nothing.
- **The ports on localhost** are `65xx` for `docker`, `66xx` for `uat` and `67xx` for `production` (gateway `00`, back office `01`, the firm invoice REST, gRPC and WCF hosts `10`, `11` and `12`, the mock center `20`, the GİB REST host `40`), and do not meet those of Abc.
- **Deploying is as in Abc**: `deploy/deploy.sh docker|uat|production`, `undeploy.sh`, `test.sh` (the same drills on invoice-service: a wrong Redis address, a wrong Kafka address, `Health:Disabled`, a gateway that answers `502` for a service that is gone), `hosts.txt` for the list of the hosts.

## Consequences

- Edx has twelve hosts and a back office; two pods of each is 26 pods in an environment, so this machine runs one of uat and production of both samples at a time (`undeploy.sh` takes the other away).
- The back office of an environment shows the data of that environment only because its build points at that environment's gateway; a build is not moved to another.
- The mock center and the GİB integration of an environment work only with each other's names: running a host of Edx in a debugger against the `docker` settings means the addresses have to be changed.

## Related

[ADR-0049](0049-the-environments-of-the-samples-are-development-docker-uat-and-production.md), [ADR-0032](0032-the-edx-back-office-an-angular-app-that-asks-the-services-what-the-user-may-do.md), framework ADR-0117.
