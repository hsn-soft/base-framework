using System.IO.Pipelines;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HsnSoft.Base.Logging.Masking;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.AspNetCore.WebApi.Tests;

[Microsoft.AspNetCore.Mvc.ApiController]
[Microsoft.AspNetCore.Mvc.Route("documents")]
public sealed class DocumentsTestController : Microsoft.AspNetCore.Mvc.ControllerBase
{
    private static readonly byte[] s_json = Encoding.UTF8.GetBytes("""{"signed":true}""");

    [Microsoft.AspNetCore.Mvc.HttpGet("anonymous")]
    public Microsoft.AspNetCore.Mvc.IActionResult Anonymous() => File(s_json, "application/json");

    [Microsoft.AspNetCore.Mvc.HttpGet("named")]
    public Microsoft.AspNetCore.Mvc.IActionResult Named() => File(s_json, "application/json", "contract.json");

    [Microsoft.AspNetCore.Mvc.HttpGet("data")]
    public Microsoft.AspNetCore.Mvc.IActionResult Data() => Ok(new { signed = true });
}

#pragma warning disable CA1835 // the array overloads of Stream are the ones under test
public sealed class RequestLoggingFilesTests
{
    private sealed record Logged(LogLevel Level, string? EventName, string Message, IReadOnlyDictionary<string, object?> Properties);

    private sealed class Capture : ILoggerProvider, ISupportExternalScope
    {
        private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

        public List<Logged> Logs { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CaptureLogger(this);

        public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

        public void Dispose()
        {
        }

        private sealed class CaptureLogger(Capture owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
                => owner._scopes.Push(state);

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                Dictionary<string, object?> properties = [];
                owner._scopes.ForEachScope(
                    (scope, target) =>
                    {
                        if (scope is IEnumerable<KeyValuePair<string, object?>> values)
                        {
                            foreach (KeyValuePair<string, object?> pair in values)
                            {
                                target[pair.Key] = pair.Value;
                            }
                        }
                    },
                    properties);
                if (state is IEnumerable<KeyValuePair<string, object?>> own)
                {
                    foreach (KeyValuePair<string, object?> pair in own)
                    {
                        properties[pair.Key] = pair.Value;
                    }
                }

                lock (owner.Logs)
                {
                    owner.Logs.Add(new Logged(logLevel, eventId.Name, formatter(state, exception), properties));
                }
            }
        }
    }

    private sealed class Masker : ILogMasker
    {
        public string MaskText(string? text) => text ?? string.Empty;

        public object MaskObject<T>(T value) => value!;
    }

    private sealed class FakeTrailersFeature : IHttpResponseTrailersFeature
    {
        public IHeaderDictionary Trailers { get; set; } = new HeaderDictionary();
    }

    private sealed class Rig(WebApplication app, Capture logs) : IAsyncDisposable
    {
        public HttpClient Client { get; } = app.GetTestClient();

        public Logged Response
        {
            get
            {
                lock (logs.Logs)
                {
                    return Assert.Single(logs.Logs, log => log.EventName == "HTTP_RESPONSE");
                }
            }
        }

        public Logged Request
        {
            get
            {
                lock (logs.Logs)
                {
                    return Assert.Single(logs.Logs, log => log.EventName == "HTTP_REQUEST");
                }
            }
        }

        public ValueTask DisposeAsync() => app.DisposeAsync();
    }

    private static readonly byte[] s_pdf = Encoding.UTF8.GetBytes(new string('p', 5000));

    private static async Task<Rig> StartAsync(bool logBodies = false, string? trailerStatus = null, string? trailerMessage = null, bool headersOnly = false)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        Capture logs = new();
        builder.Logging.ClearProviders().AddProvider(logs);
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["RequestLogging:LogBodies"] = logBodies ? "true" : "false" }).Build();
        builder.Services.AddBaseRequestLogging(configuration).AddBaseWebApi().AddSingleton<ILogMasker, Masker>();
        builder.Services.AddControllers().AddApplicationPart(typeof(DocumentsTestController).Assembly);

        WebApplication app = builder.Build();
        app.Use((context, next) =>
        {
            context.Features.Set<IHttpResponseTrailersFeature>(new FakeTrailersFeature());
            return next(context);
        });
        app.UseBaseRequestLogging();
        app.MapControllers();
        string physical = Path.Combine(Path.GetTempPath(), $"report-{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(physical, s_pdf);
        app.Lifetime.ApplicationStopped.Register(() => File.Delete(physical));
        app.MapGet("/download/bytes", () => Results.File(s_pdf, "application/pdf", "report.pdf"));
        app.MapGet("/download/stream", () => Results.Stream(new MemoryStream(s_pdf), "application/octet-stream"));
        app.MapGet("/download/physical", () => Results.File(physical, "application/octet-stream", "big.bin"));
        app.MapGet("/download/inline", context =>
        {
            context.Response.Headers.ContentDisposition = "inline; filename*=UTF-8''r%C3%A9sum%C3%A9.pdf";
            context.Response.ContentType = "application/pdf";
            return context.Response.Body.WriteAsync(s_pdf.AsMemory()).AsTask();
        });
        app.MapGet("/download/empty", context =>
        {
            context.Response.Headers.ContentDisposition = "attachment; filename=nothing.txt";
            return Task.CompletedTask;
        });
        app.MapGet("/download/pipe", async context =>
        {
            context.Response.ContentType = "application/zip";
            await context.Response.BodyWriter.WriteAsync(s_pdf);
            await context.Response.BodyWriter.FlushAsync();
        });
        app.MapGet("/download/pipe-unflushed", async context =>
        {
            // what a gRPC call does: the message is written to the pipe and the call ends, with no flush of its own
            context.Response.ContentType = "application/grpc";
            await context.Response.BodyWriter.WriteAsync(s_pdf);
        });
        app.MapGet("/json", () => Results.Text("""{"a":1}""", "application/json"));
        app.MapGet("/json-big", () => Results.Text("""{"a":"%s"}""".Replace("%s", new string('x', 600 * 1024), StringComparison.Ordinal), "application/json"));
        app.MapGet("/json-gzip", context =>
        {
            context.Response.Headers.ContentEncoding = "gzip";
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("not really gzip");
        });
        app.MapPost("/upload/form", async (HttpRequest request) =>
        {
            IFormCollection form = await request.ReadFormAsync();
            return Results.Ok(new { files = form.Files.Count });
        });
        app.MapPost("/upload/ignored", () => Results.Ok());
        app.MapPost("/upload/raw", async (HttpRequest request) =>
        {
            using MemoryStream copy = new();
            await request.Body.CopyToAsync(copy);
            return Results.Ok(new { bytes = copy.Length });
        });
        app.MapPost("/orders.Orders/GetOrder", async context =>
        {
            await context.Request.Body.CopyToAsync(Stream.Null);
            if (headersOnly)
            {
                context.Response.Headers.GrpcStatus = trailerStatus;
                context.Response.Headers.GrpcMessage = trailerMessage;
            }
            else if (trailerStatus is not null)
            {
                context.Features.Get<IHttpResponseTrailersFeature>()!.Trailers.GrpcStatus = trailerStatus;
                context.Features.Get<IHttpResponseTrailersFeature>()!.Trailers.GrpcMessage = trailerMessage;
            }

            context.Response.ContentType = "application/grpc";
            await context.Response.Body.WriteAsync(new byte[] { 0, 0, 0, 0, 0 });
        });
        await app.StartAsync();
        return new Rig(app, logs);
    }

    [Theory]
    [InlineData("/download/bytes", "report.pdf")]
    [InlineData("/download/physical", "big.bin")]
    [InlineData("/download/inline", "résumé.pdf")]
    public async Task ADownload_IsLoggedWithItsNameAndSize_AndNeverItsBody(string path, string fileName)
    {
        await using Rig rig = await StartAsync(logBodies: true);

        HttpResponseMessage response = await rig.Client.GetAsync(path);

        Assert.Equal(s_pdf.Length, (await response.Content.ReadAsByteArrayAsync()).Length);
        Logged log = rig.Response;
        Assert.Equal($", download {fileName} ({s_pdf.Length} bytes)", log.Message[(log.Message.IndexOf(" ms", StringComparison.Ordinal) + 3)..]);
        Assert.Equal(1, log.Properties["FileCount"]);
        Assert.Equal((long)s_pdf.Length, log.Properties["FileBytes"]);
        Assert.Equal((long)s_pdf.Length, log.Properties["ResponseBytes"]);
        Assert.Contains($"\"fileName\":\"{fileName}\"", (string)log.Properties["Files"]!, StringComparison.Ordinal);
        Assert.Null(log.Properties["ResponseBody"]);
    }

    [Theory]
    [InlineData("/documents/anonymous", ", download (no name) (15 bytes)")]
    [InlineData("/documents/named", ", download contract.json (15 bytes)")]
    public async Task AFileTheActionReturned_IsAFile_EvenWhenItIsJson(string path, string details)
    {
        await using Rig rig = await StartAsync(logBodies: true);

        await rig.Client.GetAsync(path);

        Assert.EndsWith(details, rig.Response.Message, StringComparison.Ordinal);
        Assert.Null(rig.Response.Properties["ResponseBody"]);
    }

    [Fact]
    public async Task AJsonAnswerOfAnAction_IsNotAFile()
    {
        await using Rig rig = await StartAsync();

        await rig.Client.GetAsync("/documents/data");

        Assert.Null(rig.Response.Properties["Files"]);
    }

    [Fact]
    public async Task ADownloadWithoutAName_IsLoggedBySizeAndKind_WhenItIsBinary()
    {
        await using Rig rig = await StartAsync();

        await rig.Client.GetAsync("/download/stream");

        Assert.EndsWith($", download (no name) ({s_pdf.Length} bytes)", rig.Response.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ADownloadWrittenThroughThePipe_IsCounted()
    {
        await using Rig rig = await StartAsync();

        await rig.Client.GetAsync("/download/pipe");

        Assert.Equal((long)s_pdf.Length, rig.Response.Properties["FileBytes"]);
    }

    [Fact]
    public async Task WhatIsWrittenToThePipeWithoutAFlush_StillReachesTheClient()
    {
        await using Rig rig = await StartAsync();

        byte[] body = await (await rig.Client.GetAsync("/download/pipe-unflushed")).Content.ReadAsByteArrayAsync();

        Assert.Equal(s_pdf, body);
    }

    [Fact]
    public async Task ResponseBodyTee_FlushWriterAsync_SendsWhatWaitsInTheWriter_AndDoesNothingWithoutOne()
    {
        FakeBody inner = new();
        ResponseBodyTee tee = new(inner, () => false, maxCapture: 10);
        await tee.FlushWriterAsync();

        IHttpResponseBodyFeature feature = tee;
        new byte[] { 1, 2, 3 }.CopyTo(feature.Writer.GetMemory(3));
        feature.Writer.Advance(3);
        Assert.Equal(0, tee.Count);
        await tee.FlushWriterAsync();

        Assert.Equal(3, tee.Count);
        Assert.Equal([1, 2, 3], inner.Written.ToArray());
    }

    [Fact]
    public async Task AnEmptyDownload_IsNotAFile()
    {
        await using Rig rig = await StartAsync();

        await rig.Client.GetAsync("/download/empty");

        Assert.Null(rig.Response.Properties["Files"]);
        Assert.EndsWith(" ms", rig.Response.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AFormUpload_IsLoggedWithTheNameAndSizeOfEveryFile()
    {
        await using Rig rig = await StartAsync(logBodies: true);
        using MultipartFormDataContent form = new();
        ByteArrayContent first = new(Encoding.UTF8.GetBytes("hello"));
        first.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(first, "document", "a.txt");
        form.Add(new ByteArrayContent(new byte[2048]), "scan", "scan.bin");
        form.Add(new StringContent("plain value"), "note");

        HttpResponseMessage response = await rig.Client.PostAsync("/upload/form", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Logged log = rig.Response;
        Assert.Contains(", upload a.txt (5 bytes); upload scan.bin (2048 bytes)", log.Message, StringComparison.Ordinal);
        Assert.Equal(2, log.Properties["FileCount"]);
        Assert.Equal(2053L, log.Properties["FileBytes"]);
        Assert.Contains("\"name\":\"document\"", (string)log.Properties["Files"]!, StringComparison.Ordinal);
        Assert.Null(rig.Request.Properties["RequestBody"]);
    }

    [Fact]
    public async Task AFormTheApplicationNeverRead_IsNotBufferedJustToLogIt()
    {
        await using Rig rig = await StartAsync();
        using MultipartFormDataContent form = new() { { new ByteArrayContent(new byte[100]), "document", "a.txt" } };

        await rig.Client.PostAsync("/upload/ignored", form);

        Assert.Null(rig.Response.Properties["Files"]);
    }

    [Fact]
    public async Task ARawUpload_IsCountedAndNamedByItsContentDisposition()
    {
        await using Rig rig = await StartAsync();
        ByteArrayContent content = new(new byte[3000]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        HttpRequestMessage request = new(HttpMethod.Post, "/upload/raw") { Content = content };
        request.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "\"backup.zip\"" };

        await rig.Client.SendAsync(request);

        Assert.Contains(", upload backup.zip (3000 bytes)", rig.Response.Message, StringComparison.Ordinal);
        Assert.Equal(3000L, rig.Request.Properties["RequestBytes"]);
    }

    [Fact]
    public async Task ARawUploadWithoutAName_OrWithABrokenOne_IsStillCounted()
    {
        await using Rig rig = await StartAsync();
        ByteArrayContent content = new(new byte[10]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        HttpRequestMessage request = new(HttpMethod.Post, "/upload/raw") { Content = content };
        request.Headers.TryAddWithoutValidation("Content-Disposition", "not;a;valid;;header=");

        await rig.Client.SendAsync(request);

        Assert.Contains(", upload (no name) (10 bytes)", rig.Response.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AJsonRequest_IsNotAFileUpload()
    {
        await using Rig rig = await StartAsync();

        await rig.Client.PostAsync("/upload/raw", new StringContent("""{"a":1}""", Encoding.UTF8, "application/json"));

        Assert.Null(rig.Response.Properties["Files"]);
        Assert.Equal(7L, rig.Request.Properties["RequestBytes"]);
        Assert.False(rig.Response.Properties.ContainsKey("RequestBytes"));
    }

    [Fact]
    public async Task ABigJsonAnswer_IsCounted_ButNotKeptForTheLog()
    {
        await using Rig rig = await StartAsync(logBodies: true);

        HttpResponseMessage response = await rig.Client.GetAsync("/json-big");
        await response.Content.ReadAsByteArrayAsync();

        Assert.Null(rig.Response.Properties["ResponseBody"]);
        Assert.True((long)rig.Response.Properties["ResponseBytes"]! > 600 * 1024);
    }

    [Fact]
    public async Task ACompressedJsonAnswer_IsNotKeptForTheLog()
    {
        await using Rig rig = await StartAsync(logBodies: true);

        await rig.Client.GetAsync("/json-gzip");

        Assert.Null(rig.Response.Properties["ResponseBody"]);
    }

    [Fact]
    public async Task ASmallJsonAnswer_IsStillLoggedWhenBodiesAreOn()
    {
        await using Rig rig = await StartAsync(logBodies: true);

        await rig.Client.GetAsync("/json");

        Assert.Equal("""{"a":1}""", rig.Response.Properties["ResponseBody"]);
        Assert.Equal(7L, rig.Response.Properties["ResponseBytes"]);
    }

    [Theory]
    [InlineData("0", null, LogLevel.Information, ", grpc-status 0")]
    [InlineData("5", "order not found", LogLevel.Warning, ", grpc-status 5 (order not found)")]
    [InlineData("13", "boom", LogLevel.Error, ", grpc-status 13 (boom)")]
    public async Task AGrpcCall_IsNamedServiceDashMethod_AndJudgedByItsGrpcStatus(string status, string? message, LogLevel level, string details)
    {
        await using Rig rig = await StartAsync(trailerStatus: status, trailerMessage: message);

        HttpRequestMessage request = new(HttpMethod.Post, "/orders.Orders/GetOrder") { Content = new ByteArrayContent([0, 0, 0, 0, 2, 8, 1]) };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc");
        await rig.Client.SendAsync(request);

        Assert.Equal("orders-getorder", rig.Request.Properties["To"]);
        Assert.Equal(level, rig.Response.Level);
        Assert.EndsWith(details, rig.Response.Message, StringComparison.Ordinal);
        Assert.Equal(int.Parse(status, System.Globalization.CultureInfo.InvariantCulture), rig.Response.Properties["GrpcStatus"]);
        Assert.Equal(7L, rig.Request.Properties["RequestBytes"]);
        Assert.False(rig.Response.Properties.ContainsKey("RequestBytes"));
        Assert.Null(rig.Response.Properties["Files"]);
    }

    [Fact]
    public async Task AGrpcCallThatFailedBeforeItsFirstMessage_HasItsStatusInTheHeaders()
    {
        await using Rig rig = await StartAsync(trailerStatus: "16", trailerMessage: "no token", headersOnly: true);

        HttpRequestMessage request = new(HttpMethod.Post, "/orders.Orders/GetOrder") { Content = new ByteArrayContent([0, 0, 0, 0, 0]) };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc+proto");
        await rig.Client.SendAsync(request);

        Assert.Equal(LogLevel.Warning, rig.Response.Level);
        Assert.EndsWith(", grpc-status 16 (no token)", rig.Response.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AGrpcCallWithoutAStatus_IsJudgedByItsHttpStatus()
    {
        await using Rig rig = await StartAsync();

        HttpRequestMessage request = new(HttpMethod.Post, "/orders.Orders/GetOrder") { Content = new ByteArrayContent([0, 0, 0, 0, 0]) };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc");
        await rig.Client.SendAsync(request);

        Assert.Equal(LogLevel.Information, rig.Response.Level);
        Assert.Null(rig.Response.Properties["GrpcStatus"]);
    }

    [Fact]
    public async Task AGrpcPathThatIsNotServiceSlashMethod_IsNamedByItsPath()
    {
        await using Rig rig = await StartAsync();

        HttpRequestMessage request = new(HttpMethod.Post, "/a/b/c") { Content = new ByteArrayContent([0]) };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc");
        await rig.Client.SendAsync(request);

        Assert.Equal("/a/b/c", rig.Request.Properties["To"]);
    }

    // ----- the streams, directly -----

    private sealed class FakeBody : IHttpResponseBodyFeature
    {
        public MemoryStream Written { get; } = new();

        public bool Started { get; private set; }

        public bool Completed { get; private set; }

        public bool Unbuffered { get; private set; }

        public string? SentFile { get; private set; }

        public Stream Stream => Written;

        public PipeWriter Writer => PipeWriter.Create(Written, new StreamPipeWriterOptions(leaveOpen: true));

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            Started = true;
            return Task.CompletedTask;
        }

        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
        {
            SentFile = path;
            return Task.CompletedTask;
        }

        public Task CompleteAsync()
        {
            Completed = true;
            return Task.CompletedTask;
        }

        public void DisableBuffering() => Unbuffered = true;
    }

    [Fact]
    public async Task ResponseBodyTee_PassesEverythingOnAndCountsIt()
    {
        FakeBody inner = new();
        ResponseBodyTee tee = new(inner, () => true, maxCapture: 100);
        IHttpResponseBodyFeature feature = tee;

        tee.Write([1, 2, 3], 0, 3);
        tee.Write([4]);
        await tee.WriteAsync([5, 6], 0, 2, CancellationToken.None);
        await feature.Writer.WriteAsync(new byte[] { 7 });
        tee.Flush();
        await tee.FlushAsync(CancellationToken.None);
        await feature.StartAsync();
        feature.DisableBuffering();
        await feature.CompleteAsync();

        Assert.Equal(7, tee.Count);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7], inner.Written.ToArray());
        Assert.True(inner.Started);
        Assert.True(inner.Unbuffered);
        Assert.True(inner.Completed);
        Assert.Same(tee, feature.Stream);
    }

    [Fact]
    public async Task ResponseBodyTee_SendFileIsCountedByTheSizeOfWhatIsSent()
    {
        string path = Path.Combine(Path.GetTempPath(), $"tee-{Guid.NewGuid():N}.bin");
        await File.WriteAllBytesAsync(path, new byte[1000]);
        try
        {
            FakeBody inner = new();
            ResponseBodyTee tee = new(inner, () => false, maxCapture: 100);

            await tee.SendFileAsync(path, 100, null);
            await tee.SendFileAsync(path, 0, 50);

            Assert.Equal(950, tee.Count);
            Assert.Equal(path, inner.SentFile);
            Assert.Null(tee.Body);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ResponseBodyTee_KeepsASmallBodyAndDropsOneThatGrowsTooLong()
    {
        ResponseBodyTee small = new(new FakeBody(), () => true, maxCapture: 10);
        small.Write(Encoding.UTF8.GetBytes("abcde"));
        small.Write(Encoding.UTF8.GetBytes("fghij"));
        ResponseBodyTee big = new(new FakeBody(), () => true, maxCapture: 10);
        big.Write(Encoding.UTF8.GetBytes("abcde"));
        big.Write(Encoding.UTF8.GetBytes("fghijk"));
        big.Write(Encoding.UTF8.GetBytes("more"));

        Assert.Equal("abcdefghij", small.Body);
        Assert.Null(big.Body);
        Assert.Equal(15, big.Count);
    }

    [Fact]
    public void ResponseBodyTee_IsAWriteOnlyStream()
    {
        ResponseBodyTee tee = new(new FakeBody(), () => false, maxCapture: 1);

        Assert.True(tee.CanWrite);
        Assert.False(tee.CanRead);
        Assert.False(tee.CanSeek);
        Assert.Throws<NotSupportedException>(() => tee.Length);
        Assert.Throws<NotSupportedException>(() => tee.Position);
        Assert.Throws<NotSupportedException>(() => tee.Position = 1);
        Assert.Throws<NotSupportedException>(() => tee.Read(new byte[1], 0, 1));
        Assert.Throws<NotSupportedException>(() => tee.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => tee.SetLength(1));
    }

    [Fact]
    public async Task CountingReadStream_CountsWhatWasRead_AndKeepsTheSeekabilityOfTheBody()
    {
        using MemoryStream body = new(new byte[100]);
        using CountingReadStream counting = new(body);
        byte[] buffer = new byte[30];

        Assert.Equal(10, counting.Read(buffer, 0, 10));
        Assert.Equal(10, counting.Read(buffer.AsSpan(0, 10)));
        Assert.Equal(10, await counting.ReadAsync(buffer, 0, 10, CancellationToken.None));
        Assert.Equal(10, await counting.ReadAsync(buffer.AsMemory(0, 10)));
        counting.Seek(0, SeekOrigin.Begin);
        counting.Position = 0;
        counting.Flush();

        counting.Position = 5;
        Assert.Equal(5, counting.Position);
        Assert.Equal(40, counting.Count);
        Assert.True(counting.CanRead);
        Assert.True(counting.CanSeek);
        Assert.False(counting.CanWrite);
        Assert.Equal(100, counting.Length);
        Assert.Throws<NotSupportedException>(() => counting.SetLength(1));
        Assert.Throws<NotSupportedException>(() => counting.Write(buffer, 0, 1));
    }
}
#pragma warning restore CA1835
