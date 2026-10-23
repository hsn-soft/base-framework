using Def.Signer.Host.Events;
using Def.Signer.Host.Persistence;
using HsnSoft.Base.BlobStoring;
using HsnSoft.Base.EventBus;
using Microsoft.EntityFrameworkCore;

namespace Def.Signer.Host.Endpoints;

public sealed record CreateDocumentRequest(string Title, string? Content);

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/documents").RequireAuthorization();

        // answers as soon as Kafka has the request; the document appears later
        group.MapPost("/", async (CreateDocumentRequest request, HttpContext http, IEventBus bus, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest(new { error = "Title is required." });
            }

            var requested = new SignRequested(Owner(http), request.Title.Trim(), request.Content ?? string.Empty);
            await bus.PublishAsync(requested, cancellationToken);
            return Results.Accepted($"/documents/{requested.EventId}", new { requestId = requested.EventId, status = "queued" });
        });

        group.MapGet("/", async (HttpContext http, SignerDbContext db, CancellationToken cancellationToken) =>
        {
            var owner = Owner(http);
            return await db.Documents.AsNoTracking()
                .Where(x => x.Owner == owner)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.RequestId, x.Title, x.SizeBytes, x.CreatedAt })
                .ToListAsync(cancellationToken);
        });

        // 404 while the request is still queued: poll until it is there
        group.MapGet("/{requestId:guid}", async (Guid requestId, HttpContext http, SignerDbContext db, CancellationToken cancellationToken) =>
        {
            var owner = Owner(http);
            var document = await db.Documents.AsNoTracking()
                .FirstOrDefaultAsync(x => x.RequestId == requestId && x.Owner == owner, cancellationToken);
            return document is null ? Results.NotFound() : Results.Ok(document);
        });

        group.MapGet("/{requestId:guid}/content", async (Guid requestId, HttpContext http, SignerDbContext db, IBlobStore blobs, CancellationToken cancellationToken) =>
        {
            var owner = Owner(http);
            var document = await db.Documents.AsNoTracking()
                .FirstOrDefaultAsync(x => x.RequestId == requestId && x.Owner == owner, cancellationToken);
            if (document is null)
            {
                return Results.NotFound();
            }

            var content = await blobs.OpenReadAsync(document.Container, document.BlobName, cancellationToken);
            return content is null ? Results.NotFound() : Results.File(content, document.ContentType);
        });

        return app;
    }

    private static string Owner(HttpContext http) => http.User.Identity!.Name!;
}
