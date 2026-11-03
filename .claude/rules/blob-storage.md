---
paths:
  - "src/HsnSoft.Base.BlobStoring/**"
  - "src/HsnSoft.Base.BlobStoring.*/**"
---

# Blob storage

- Keep `IBlobStore` provider-neutral and each storage backend in its own opt-in provider package. Do not make the abstraction depend on MinIO, Azure, AWS, or an S3 client.
- Preserve streaming reads and bounded handling of uploads that cannot seek: keep up to 1 MB in memory and spill larger bodies to a disposable temporary file, with cleanup on every path.
- Follow each provider's timeout/retry configuration; S3/MinIO total request timeouts are opt-in, not universal defaults. Do not buffer an entire large blob in memory or silently convert a failed upload/read into success.
- Test provider behavior against the real service with the existing Testcontainers/integration-test conventions. Read the relevant storage provider docs and ADR before changing its wire or resource lifecycle.

Relevant decisions: ADR-0003, ADR-0076, ADR-0112, ADR-0140.
