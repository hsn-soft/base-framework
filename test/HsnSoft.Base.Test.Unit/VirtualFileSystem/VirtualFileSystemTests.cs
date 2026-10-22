using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using HsnSoft.Base.Json;
using HsnSoft.Base.Json.Newtonsoft;
using HsnSoft.Base.Json.Newtonsoft.Converters;
using HsnSoft.Base.Json.SystemTextJson;
using HsnSoft.Base.Json.SystemTextJson.JsonConverters;
using HsnSoft.Base.Timing;
using HsnSoft.Base.VirtualFileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using MsOptions = Microsoft.Extensions.Options.Options;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;
using NewtonsoftJson = Newtonsoft.Json.JsonConvert;

namespace HsnSoft.Base.Test.Unit.VirtualFileSystem;

public class VirtualFileSystemTests
{
    // ---------- VirtualFileSystem ----------

    [Fact]
    public void EmbeddedFileProviderQuery_Works()
    {
        var q = new EmbeddedFileProviderQuery();
        var asm = typeof(VirtualFileSystemTests).Assembly;
        static string Read(Stream s) { using var r = new StreamReader(s); return r.ReadToEnd(); }

        Read(q.Read<VirtualFileSystemTests>("Resources/embedded.txt")).Should().Be("embedded-content");
        Read(q.Read(asm, "Resources/embedded.txt")).Should().Be("embedded-content");
        Read(q.Read(asm.GetName().Name!, "Resources/embedded.txt")).Should().Be("embedded-content");
    }

    [Fact]
    public void EmbeddedResourceQuery_Works()
    {
        var q = new EmbeddedResourceQuery();
        var asm = typeof(VirtualFileSystemTests).Assembly;
        static string Read(Stream s) { using var r = new StreamReader(s); return r.ReadToEnd(); }
        var ns = asm.GetName().Name + ".Resources";

        Read(q.Read(asm, "embedded.txt", ns)).Should().Be("embedded-content");
        Read(q.Read(asm.GetName().Name!, "embedded.txt", ns)).Should().Be("embedded-content");
        q.Read(asm, "Resources.embedded.txt").Should().NotBeNull();
        q.Read(asm.GetName().Name!, "Resources.embedded.txt").Should().NotBeNull();
        q.Read<VirtualFileSystemTests>("missing.txt").Should().BeNull();
    }
}
