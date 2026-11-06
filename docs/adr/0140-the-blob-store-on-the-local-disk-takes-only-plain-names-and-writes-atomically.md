# ADR-0140: The blob store on the local disk takes only plain names and writes atomically (`BlobStoring.FileSystem`)

- **Status:** Accepted
- **Date:** 2026-11-03

## Context

The old framework had a `BlobStoring.FileSystem` project that was a project file with no code ([the analysis](../old-framework/3-messaging-storage.md)); the new tree has the MinIO, Azure and AWS providers ([ADR-0076](0076-more-providers-and-the-rules-every-provider-shares.md), [ADR-0112](0112-blob-reads-stream-and-uploads-that-cannot-seek-spill-to-disk.md)) and nothing for a developer machine or a single-node deployment, where a bucket is more than is needed. A provider on the disk has a danger the others do not: the name of a blob comes from a request, and a name that reaches outside the root (`../`, an absolute path, a link) reads or writes any file of the machine.

## Decision

- **`HsnSoft.Base.BlobStoring.FileSystem`** implements `IBlobStore` on a root folder (`FileSystem:RootPath`, required and absolute, created when missing); a container is a folder under it and a blob a file in it, a name with `/` being folders. It is opt-in (`AddFileSystemBlobStoring`) with a readiness check that writes and removes a probe file in the root.
- **Only plain names are taken** (`BlobPaths`), the same on every system: no `.` or `..` part, no `\`, no control character, none of `: * ? " < > |`, no part that ends with a dot or a space or starts with a space, no device name (`CON`, `NUL`, `COM1`...), no part longer than 255, none that starts with the prefix of the temporary files (`.hsn-blob-`). The resolved path is checked to be under the root as well, and **a folder from the root down that is a link is refused**, so a link cannot take a write or a read outside the root.
- **A write is atomic and streamed**: the content is copied as it arrives (never held in memory, whatever its size) to a temporary file in the folder of the blob and moved over the blob only when complete; a write that fails or is cancelled removes its temporary file and keeps the old blob; a reader sees the old blob or the whole new one, and a blob that is being read can be replaced.
- The content type is not kept (a disk has no place for it); a service that needs it stores it itself.

## Consequences

A development machine or a single node needs no object store; the same code moves to MinIO or S3 by one registration. The cost: the names a service uses must be plain (a name with `?` or a trailing dot that S3 would take is refused here, on purpose, so the code works on both); the disk is one machine's, so more than one node needs a shared volume or a bucket; there is no listing, as `IBlobStore` has none.

## Related

[ADR-0003](0003-abstractions-separate-from-providers.md), [ADR-0076](0076-more-providers-and-the-rules-every-provider-shares.md), [ADR-0112](0112-blob-reads-stream-and-uploads-that-cannot-seek-spill-to-disk.md), [the old framework analysis](../old-framework/README.md).
