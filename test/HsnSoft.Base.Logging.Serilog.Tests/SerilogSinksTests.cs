using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace HsnSoft.Base.Logging.Serilog.Tests;

/// <summary>The real sinks: the console templates, the file, and Graylog against a GELF server of our own.</summary>
[Collection(ProcessWideCollection.Name)]
public sealed class SerilogSinksTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("hsn-log-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private sealed class GelfServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly List<string> _bodies = [];
        private readonly Task _loop;

        public GelfServer()
        {
            TcpListener probe = new(IPAddress.Loopback, 0);
            probe.Start();
            Port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();
            _loop = Task.Run(Serve);
        }

        public int Port { get; }

        public IReadOnlyList<string> Bodies
        {
            get
            {
                lock (_bodies)
                {
                    return [.. _bodies];
                }
            }
        }

        public string? Path { get; private set; }

        public void Dispose()
        {
            _listener.Close();
            try
            {
                _loop.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException)
            {
                // the listener was closed while it waited
            }
        }

        private async Task Serve()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception exception) when (exception is HttpListenerException or ObjectDisposedException)
                {
                    return;
                }

                Path = context.Request.Url?.AbsolutePath;
                Stream body = context.Request.InputStream;
                if (string.Equals(context.Request.Headers["Content-Encoding"], "gzip", StringComparison.OrdinalIgnoreCase))
                {
                    body = new GZipStream(body, CompressionMode.Decompress);
                }

                using StreamReader reader = new(body, Encoding.UTF8);
                string text = await reader.ReadToEndAsync();
                lock (_bodies)
                {
                    _bodies.Add(text);
                }

                context.Response.StatusCode = 202;
                context.Response.Close();
            }
        }
    }

    private static LogEvent Event(LogEventLevel level, string message, params (string Name, object Value)[] properties)
        => new(
            DateTimeOffset.UtcNow,
            level,
            null,
            new MessageTemplateParser().Parse(message),
            [
                new LogEventProperty("LoggerName", new ScalarValue("Host")),
                new LogEventProperty(Constants.SourceContextPropertyName, new ScalarValue("My.Source")),
                .. properties.Select(property => new LogEventProperty(property.Name, new ScalarValue(property.Value))),
            ]);

    private FrameworkLoggerOptions Options(Action<FrameworkLoggerOptions>? configure = null)
    {
        FrameworkLoggerOptions options = new() { FilePath = Path.Combine(_directory, "app.txt") };
        configure?.Invoke(options);
        return options;
    }

    [Fact]
    public void WithoutGraylog_PersistentLogsGoToTheFile()
    {
        SerilogSinks sinks = SerilogSinks.Create(Options());

        sinks.Persistent.Emit(Event(LogEventLevel.Warning, "written to the file"));
        (sinks.Persistent as IDisposable)?.Dispose();

        string file = Assert.Single(Directory.GetFiles(_directory, "app*.txt"));
        string content = ReadShared(file);
        Assert.Contains("WRN", content, StringComparison.Ordinal);
        Assert.Contains("written to the file", content, StringComparison.Ordinal);
        Assert.Contains("My.Source", content, StringComparison.Ordinal);
        Assert.Null(sinks.Refreshable);
    }

    [Fact]
    public void Console_PrintsTheLevelTheNameAndTheMessage()
    {
        TextWriter original = Console.Out;
        using StringWriter captured = new();
        Console.SetOut(captured);
        try
        {
            SerilogSinks sinks = SerilogSinks.Create(Options());

            sinks.Console.Emit(Event(LogEventLevel.Error, "plain message", ("Extra", "hidden-in-plain")));
            sinks.ConsoleWithProperties.Emit(Event(LogEventLevel.Information, "structured message", ("Extra", "shown-with-properties")));
        }
        finally
        {
            Console.SetOut(original);
        }

        string output = captured.ToString();
        Assert.Contains("plain message", output, StringComparison.Ordinal);
        Assert.Contains("ERR", output, StringComparison.Ordinal);
        Assert.Contains("Host", output, StringComparison.Ordinal);
        Assert.DoesNotContain("hidden-in-plain", output, StringComparison.Ordinal);
        Assert.Contains("structured message", output, StringComparison.Ordinal);
        Assert.Contains("shown-with-properties", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Graylog_ReceivesPersistentLogsOverHttp()
    {
        using GelfServer server = new();
        SerilogSinks sinks = SerilogSinks.Create(Options(options =>
        {
            options.IsGrayLogActive = true;
            options.GrayLog.Address = "http://127.0.0.1";
            options.GrayLog.Port = server.Port;
        }));

        sinks.Persistent.Emit(Event(LogEventLevel.Error, "sent to graylog", ("CorrelationId", "corr-7")));
        Wait(() => server.Bodies.Count > 0);
        sinks.Refreshable!.Dispose();

        string body = Assert.Single(server.Bodies);
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;
        Assert.Equal("sent to graylog", root.GetProperty("short_message").GetString());
        Assert.Equal(3, root.GetProperty("level").GetInt32());
        Assert.Equal("corr-7", root.GetProperty("_CorrelationId").GetString());
        Assert.Equal("/gelf", server.Path);
    }

    [Fact]
    public void Graylog_WithoutAnAddress_IsAConfigurationError()
    {
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => SerilogSinks.Create(Options(options => options.IsGrayLogActive = true)));

        Assert.Contains("GrayLog:Address", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Graylog_ThatIsNotThere_DoesNotBreakTheApplication()
    {
        SerilogSinks sinks = SerilogSinks.Create(Options(options =>
        {
            options.IsGrayLogActive = true;
            options.GrayLog.Address = "http://127.0.0.1";
            options.GrayLog.Port = 1;
        }));

        sinks.Persistent.Emit(Event(LogEventLevel.Error, "nobody listens"));
        sinks.Refreshable!.Refresh();
        sinks.Refreshable.Dispose();
    }

    private static string ReadShared(string path)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    private static void Wait(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(50);
        }
    }
}
