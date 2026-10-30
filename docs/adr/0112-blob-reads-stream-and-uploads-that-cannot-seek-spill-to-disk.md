# ADR-0112: Reading a blob streams it; a body that cannot seek goes to memory up to a megabyte and to a temporary file beyond

- **Status:** Accepted
- **Date:** 2026-10-30 (adds to the blob stores of [ADR-0070](0070-a-framework-package-brings-what-its-api-exposes.md) and [ADR-0002](0002-one-package-one-capability.md))

## Context

Two things of the MinIO and the AWS S3 stores held a whole object in memory. **Reading from MinIO**: the client of MinIO gives the body to a callback, and the callback copied all of it into a `MemoryStream` before the caller got a stream (opening and reading 1 KB of a 96 MB object allocated 256 MB, with the
doubling of the buffer); a few reads of large objects at the same time ran a pod out of its memory limit. **Saving a stream that cannot seek** (a request body) to MinIO or AWS S3: both want the size before the upload, and the stores copied the body into a `MemoryStream` to know it. The Azure store and the read of the AWS store
already streamed.

## Decision

- **`MinioBlobStore.OpenReadAsync` streams.** The callback writes into a pipe (`System.IO.Pipelines`) whose writer waits when 4 MB are not read yet and goes on at 2 MB, and the caller reads the other end: only a few megabytes of the object are in memory, whatever its size. Opening waits for the first bytes (or the end), so an object or a
  bucket that is not there is still `null` and a failure before the body is thrown when the object is opened; a failure after the first bytes is thrown by the read. Disposing the stream cancels the download and lets go of the pipe. The caller's cancellation ends the opening only; the stream owns the download after that.
- **`SeekableContent.EnsureSeekableAsync`** (package `HsnSoft.Base.BlobStoring`, used by both stores) makes a stream that cannot seek one that can without holding it all: **up to 1 MB stays in memory** (the common small upload costs what it did), a larger one is copied to a **temporary file** (`hsnsoft-blob-*.tmp` in the temporary folder) that is deleted when the stream
  is disposed, whether the upload succeeded or not. A stream that can seek is used as it is.
- The package `HsnSoft.Base.BlobStoring` is no longer only an interface, so it **has a unit test project** and is out of the exemption list of `ci-check-test-project-exist.sh`.

## Consequences

- The memory of a read or an upload of a large object is bounded by the window of the pipe (a few megabytes) and the buffers of the client, not by the object. An upload of a large body needs **free disk in the temporary folder** (an `emptyDir` or the writable layer of the container) for the size of that body; a body that cannot be spilled fails with an `IOException`.
- A read pays a little more for the first bytes (one task and a pipe) than a buffered one; a large object pays much less.
- Tests: the backpressure of the pipe (a 512 MB body is not downloaded ahead of a reader that read 1 KB), the end of the download on disposal, null, failures before and after the first bytes, cancellation (unit); the spill, its cleanup on success, on failure and on cancellation (unit); on real MinIO, AWS S3 and Azurite, a 48 MB body that cannot seek round trips with no temporary file left, and opening and
  reading 1 KB of a 96 MB blob allocates less than 32 MB (the buffered read allocated 256 MB: the test fails on it).

## Related

[ADR-0010](0010-integration-tests-with-testcontainers.md), [ADR-0110](0110-integration-tests-share-a-container-per-service-and-own-their-data.md).
