# ADR-0108: The MinIO image of the integration tests is the one Chainguard builds

- **Status:** Accepted
- **Date:** 2026-10-30 (refines [ADR-0010](0010-integration-tests-with-testcontainers.md))

## Context

The MinIO and AWS S3 stores are tested against a real MinIO (Testcontainers). The image, `minio/minio:RELEASE.2024-08-29T01-40-52Z`, can no longer be pulled: Docker Hub answers `denied`, and quay.io has neither that tag nor `latest`. The tests passed on every developer machine, which had the image
in the cache, and failed on the CI runner, which pulls everything: of 85 integration tests 73 passed and the 12 of the two blob classes (six contract tests each) could not start their container.

## Decision

- **The tests use `cgr.dev/chainguard/minio:latest`** (`Support.MinioImage` is the one place that names it): MinIO built from its source by Chainguard, pulled without a login. A copy kept in the registry of the organization was the first choice and was dropped: it needs a one-time push by a person with a copy of the old image and
  a package to look after, for an image that is only a test double.
- **The image is not pinned.** Chainguard keeps the latest build of an image. A MinIO that changes its behavior shows up as a failing blob test, which is the signal the tests exist for.
- **The other test images stay where they are** (Docker Hub, `mcr.microsoft.com`): they are still published and the CI pulled them.

## Consequences

- The integration tests run on a clean machine again without anything of ours to keep, and a developer machine pulls the image on the first run.
- A new build of the image can break the two blob classes on a day nobody changed anything; the fallback is a copy kept in a registry of ours (the first choice above), made when it happens.
- The tests were only ever run on machines that had the image: **a clean machine is the real test**, and the CI is that machine.

## Related

[ADR-0010](0010-integration-tests-with-testcontainers.md), [ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md).
