# ADR-0047: The Abc system runs in Docker Compose and in Kubernetes

- **Status:** Accepted
- **Date:** 2026-10-31 (uses the `docker` environment of [ADR-0016](0016-environments-of-the-abc-sample.md); the probes are the ones of framework ADR-0117, which links to `../../../docs/adr`)

## Context

The Abc hosts only ran as processes on a developer's machine (`run-all.sh`). Nothing showed how they behave as pods: whether the probes of the health endpoints take a pod out of the traffic and put it back, whether a stop
waits for what a host is handling, whether the secrets can stay out of the files. The `docker` environment of the hosts (the names of a compose network, port 8080, `CHANGE_ME` for every password) was ready for it.

## Decision

- **`samples/abc/deploy/` holds all of it:** one `Dockerfile` for every host (the host is a build argument; the context is the root of the repository because the hosts reference `src/`; `Dockerfile.dockerignore` lets in only what the build needs and
  **keeps the private key out of the image**), `build-images.sh` (images `abc/<name>:dev`), `docker-compose.yml`, `k8s/` (a kustomization with the namespace, the infrastructure and the hosts) and the scripts `k8s-up.sh`, `k8s-down.sh` and `k8s-test.sh`.
- **No password is in a file that is committed.** `make-env.sh` writes random passwords to `deploy/.env` (git ignores it) and makes the keys of the tokens if they are not there; the compose file and `k8s-up.sh` (which makes the secret `abc-secrets` from it) put the
  passwords into the connection strings through the environment of the hosts. The private key of the auth server is a secret mounted at `/app/private_key.xml`.
- **The manifests carry the choices of a platform:** the three probes of the health endpoints (the startup probe gives a start up to three minutes, the readiness probe takes a pod that cannot reach a dependency out of the traffic, the liveness probe asks only the process),
  a probe timeout longer than `Health:CheckTimeout`, `terminationGracePeriodSeconds` of 80 (the host waits up to 30 s for its hosted services and the event bus up to 30 s, and the preStop 5 s), and requests and limits.
- **Every host has a `Health` section in its `appsettings.json`** (`CheckTimeout`, `Disabled`): where a service says which of its checks it does not run. In a cluster the same is an environment variable (`Health__Disabled__0=redis`).
- **`k8s-test.sh` is the proof:** it asks every host its three endpoints, takes Redis and then RabbitMQ away and checks that the services which wait for them leave the traffic without being restarted and come back, that `Health:Disabled` keeps a service ready, and that a gateway
  stays ready and answers `502` for a service that is gone.
- The infrastructure in the manifests has one pod of each and no volume (the data goes with the pod): it is for trying, not a recommendation for a system that keeps data.

## Consequences

- The system starts with `make-env.sh`, `build-images.sh` and either `docker compose up` or `k8s-up.sh`; the images use the local Docker, so Docker Desktop's Kubernetes needs no registry.
- A change of a host is `build-images.sh <name>` and `kubectl -n abc rollout restart deployment/<name>`.
- The SQL Server of the sample is reached as `sa` in these two setups (the `abc_app` user of the docker settings has no login there); the connection strings are overridden, the committed files are not changed.

## Related

[ADR-0016](0016-environments-of-the-abc-sample.md), [ADR-0002](0002-gateways-per-audience.md), framework ADR-0117, `abc/deploy/`.
