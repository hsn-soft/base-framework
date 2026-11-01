# ADR-0059: Hsx has the text normalizer and the video generator on MongoDB, and the four environments

- **Status:** Accepted
- **Date:** 2026-11-01 (extends [ADR-0058](0058-hsx-imitates-hhs-to-test-the-new-framework.md); applies [ADR-0049](0049-the-environments-of-the-samples-are-development-docker-uat-and-production.md))

## Decision

- **Two more services of hhs**, each with the four projects of every microservice ([ADR-0005](0005-microservice-project-pattern.md)) and the persistence on **MongoDB** (as in hhs): `Hsx.TextNormalizerService` (`CustomerContentNormalizedRequest`, route `text-normalizer-service/v1/customer-content-normalized-requests`, port 7460, database `Hsx_TextNormalizer_*`)
  and `Hsx.VideoGeneratorService` (`VideoRequest`, `video-generator-service/v1/video-requests`, port 7470, `Hsx_VideoGenerator_*`). Each has one plain entity with the CRUD of the other Hsx services (get, paged-list, create, update, delete) and is **scope-isolated** (`IScopeSubscription`): the Mongo
  concept of the framework filters by the keys of the token. The fields that hhs keeps in nested documents (the scraping and outline results, the polling and retry fields) and the events, the workers and the providers come later, piece by piece.
- **The gateway** routes both (`/text-normalizer-service/v1/...`, `/video-generator-service/v1/...`); the auth server issues the audiences `hsx-text-normalizer` and `hsx-video-generator`.
- **Four environments**, as Abc has: development (`run-all.sh`), `docker` (Compose), `uat` and `production` (Kubernetes of Docker Desktop, namespaces `hsx-uat` and `hsx-prod`, two pods of each host). `hsx/deploy/` holds the Dockerfile, `hosts.txt` (seven hosts), the compose file, `k8s/apps.yaml`, `make-env.sh`,
  `render-settings.sh`, `build-images.sh`, `deploy.sh`, `undeploy.sh` and `test.sh` (the health probes). The databases are `Hsx_*_Dev`, `_Uat`, `_Prod` on the infrastructure of the machine (PostgreSQL, MongoDB, Redis); there is no broker yet, so no virtual host.
- **Ports** (commercial gateway, commercial web): development and docker 7200 7300 (the same: one of the two at a time), uat 7210 7310, production 7220 7320.
- `smoke-test.sh [gateway]` also makes a request in each new service as Techsummus, checks that Techsummus and the system tenant see it and Pixel does not, and deletes it.

## Consequences

- `deploy.sh uat` and `production` need the images (`build-images.sh`) and `deploy.sh docker` stops the development hosts' ports, so stop them first.
- When a service gets events, the broker and a virtual host per environment are added as Abc has them (a later ADR).
