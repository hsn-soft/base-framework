var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Storage directory for uploaded files
var storageDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "media");
Directory.CreateDirectory(storageDir);

// Upload endpoint - accept multipart form data with file
app.MapPost("/storage/upload", async (HttpRequest request) =>
{
    try
    {
        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "Content-Type must be multipart/form-data" });

        var form = await request.ReadFormAsync();
        var file = form.Files["file"];

        if (file == null || file.Length == 0)
            return Results.BadRequest(new { error = "No file provided" });

        // Generate unique filename
        var fileName = $"{Guid.NewGuid():N}-{file.FileName}";
        var filePath = Path.Combine(storageDir, fileName);

        using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var fakeRemoteUrl = $"https://fake-storage.internal/files/{fileName}";
        return Results.Ok(new
        {
            fileName,
            url = fakeRemoteUrl,
            size = file.Length,
            contentType = file.ContentType
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Alternative upload endpoint - accept raw binary data with optional local filename
app.MapPost("/storage/upload-binary", async (HttpRequest request) =>
{
    try
    {
        var localFileName = request.Query["fileName"].ToString();

        // Generate storage filename by prefixing local filename with 'storage_'
        var storageFileName = $"storage_{localFileName}";
        var filePath = Path.Combine(storageDir, storageFileName);

        using (var stream = System.IO.File.Create(filePath))
        {
            await request.Body.CopyToAsync(stream);
        }

        var fakeRemoteUrl = $"https://fake-storage.internal/files/{storageFileName}";
        return Results.Ok(new
        {
            localFileName,
            fileName = storageFileName,
            url = fakeRemoteUrl,
            storagePath = filePath
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Download endpoint
app.MapGet("/storage/files/{fileName}", async (string fileName) =>
{
    var filePath = Path.Combine(storageDir, fileName);
    if (!System.IO.File.Exists(filePath))
        return Results.NotFound(new { error = $"File not found: {fileName}" });

    var fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
    var contentType = GetContentType(fileName);
    return Results.File(fileContent, contentType, fileName);
});

// List all uploaded files
app.MapGet("/storage/list", () =>
{
    var files = Directory.GetFiles(storageDir)
        .Select(filePath => new
        {
            fileName = Path.GetFileName(filePath),
            size = new FileInfo(filePath).Length,
            url = $"https://fake-storage.internal/files/{Path.GetFileName(filePath)}"
        })
        .ToList();

    return Results.Ok(new
    {
        storagePath = storageDir,
        fileCount = files.Count,
        files
    });
});

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", storageDir }));

app.Run();

static string GetContentType(string fileName) => Path.GetExtension(fileName).ToLower() switch
{
    ".mp3" => "audio/mpeg",
    ".mp4" => "video/mp4",
    ".txt" => "text/plain",
    ".pdf" => "application/pdf",
    _ => "application/octet-stream"
};
