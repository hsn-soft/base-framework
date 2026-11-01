# ADR-0049: The environments of the samples are development, docker, uat and production, on the infrastructure of this machine

- **Status:** Accepted
- **Date:** 2026-10-31 (replaces the environments and the infrastructure of the deployments in [ADR-0016](0016-environments-of-the-abc-sample.md) and [ADR-0047](0047-the-abc-system-runs-in-docker-compose-and-in-kubernetes.md))

## Context

The sample had five environments (`stage` was one) whose settings were fake, and a deployment that brought its own PostgreSQL, SQL Server, MongoDB, Redis and RabbitMQ in pods. A developer already runs that infrastructure on this machine, with the databases of the development
environment in it: to look at what the system did in Docker or in a cluster, the data had to be in the same MongoDB and PostgreSQL, not in throw-away pods.

## Decision

- **Four environments** (`stage` is gone): `development` (a debugger on the machine: `localhost`, the values of `launchSettings.json`), `docker` (the hosts in Docker Compose), `uat` and `production` (the hosts in the Kubernetes of Docker Desktop, namespaces `abc-uat` and `abc-prod`, **two pods of each host**).
  `appsettings.json` has what is the same everywhere, `appsettings.<environment>.json` what differs.
- **One infrastructure, the one on this machine.** The hosts of `docker`, `uat` and `production` reach it as `host.docker.internal` (the PostgreSQL, SQL Server, MongoDB, Redis and RabbitMQ the hosts use in development, and Graylog). Nothing is deployed for it.
- **The environments are told apart by what they own, not by where they run:**
  - the **databases**: `Abc_<Name>_Dev` for `development` and `docker` (the same data), `Abc_<Name>_Uat`, `Abc_<Name>_Prod`; one MongoDB or PostgreSQL shows all three;
  - the **prefix of the keys in Redis** (`Redis:InstanceName`): `abc:`, `abc-uat:`, `abc-prod:`;
  - the **virtual host of RabbitMQ** (`RabbitMq:Connection:VirtualHost`): `/` (the default one) for `development` and `docker`, `uat` and `prod`: the isolation RabbitMQ has, so the exchanges and the queues of the environments never meet, whatever they are called. `deploy.sh` makes the virtual host
    (through the management API of the broker) and gives the user of the hosts its permissions.
- **The passwords are not in the committed settings** of `docker`, `uat` and `production` (`CHANGE_ME`). `make-env.sh` reads the ones of the development settings of the same hosts into `deploy/.env` (git ignores it), `render-settings.sh` puts them in a copy of the settings of an
  environment (`deploy/.rendered/`, ignored), and that copy is what a container gets: a bind mount in Compose, a secret per host in the cluster. The committed file of an environment is the one place of its settings.
- **One command deploys an environment:** `deploy/deploy.sh docker|uat|production` (the secrets, the images, the settings, the deployment); `undeploy.sh` takes it away; `test.sh` proves the health behaviour of an environment. The list of the hosts is `deploy/hosts.txt`.
- **`test.sh` does not stop the shared infrastructure.** A dependency is taken from one deployment by giving it a wrong address (what a broken connection looks like to the platform): the new pod is alive but not ready and gets no traffic, the pods that were there keep serving.

## Consequences

- To see the three environments, open the same MongoDB or PostgreSQL: `Abc_Order_Dev`, `Abc_Order_Uat`, `Abc_Order_Prod`.
- `development` and `docker` share their data and their virtual host: run one of them at a time.
- The tokens of the environments are made by the same key pair and carry the same issuer and audience, so a token of one is valid in the others; a real system would have a pair for each.
- RabbitMQ is reached with the user the hosts use in development; a broker that lets that user connect only from the machine itself (the default of `guest`) refuses the hosts in Docker and in the cluster.

## Related

[ADR-0016](0016-environments-of-the-abc-sample.md), [ADR-0047](0047-the-abc-system-runs-in-docker-compose-and-in-kubernetes.md), framework ADR-0117.
