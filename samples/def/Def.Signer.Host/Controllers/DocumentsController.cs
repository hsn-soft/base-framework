using Def.Signer.Host.Events;
using Def.Signer.Host.Persistence;
using HsnSoft.Base;
using HsnSoft.Base.BlobStoring;
using HsnSoft.Base.EventBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Def.Signer.Host.Controllers;

public sealed record CreateDocumentRequest(string Title, string? Content);

[ApiController]
[Route("documents")]
[Authorize]
public sealed class DocumentsController(IEventBus bus, SignerDbContext db, IBlobStore blobs) : ControllerBase
{
    private string Owner => User.Identity!.Name!;

    // answers as soon as Kafka has the request; the document appears later
    [HttpPost]
    public async Task<IActionResult> CreateAsync(CreateDocumentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new BusinessRuleException("Title is required.");
        }

        SignRequested requested = new(Guid.CreateVersion7(), Owner, request.Title.Trim(), request.Content ?? string.Empty);
        await bus.PublishAsync(requested, cancellationToken: cancellationToken);
        return Accepted($"/documents/{requested.RequestId}", new { requestId = requested.RequestId, status = "queued" });
    }

    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        string owner = Owner;
        return Ok(await db.Documents.AsNoTracking()
            .Where(x => x.Owner == owner)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.RequestId, x.Title, x.SizeBytes, x.CreatedAt })
            .ToListAsync(cancellationToken));
    }

    // 404 while the request is still queued: poll until it is there
    [HttpGet("{requestId:guid}")]
    public async Task<IActionResult> GetAsync(Guid requestId, CancellationToken cancellationToken)
    {
        SignedDocument? document = await FindAsync(requestId, cancellationToken);
        return document is null ? NotFound() : Ok(document);
    }

    [HttpGet("{requestId:guid}/content")]
    public async Task<IActionResult> GetContentAsync(Guid requestId, CancellationToken cancellationToken)
    {
        SignedDocument? document = await FindAsync(requestId, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        Stream? content = await blobs.OpenReadAsync(document.Container, document.BlobName, cancellationToken);
        return content is null ? NotFound() : File(content, document.ContentType);
    }

    private Task<SignedDocument?> FindAsync(Guid requestId, CancellationToken cancellationToken)
    {
        string owner = Owner;
        return db.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.RequestId == requestId && x.Owner == owner, cancellationToken);
    }
}
