using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using BenchmarkDotNet.Attributes;
using HsnSoft.Base.BlobStoring.FileSystem;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Domain.Sorting;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.Net;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;

namespace HsnSoft.Base.Benchmarks;

/// <summary>What every log line of a request pays: hiding the sensitive values of a JSON text and of an object.</summary>
[MemoryDiagnoser]
public class MaskingBenchmarks
{
    public sealed class Login
    {
        public string Email { get; set; } = "ali@example.com";

        [SensitiveData]
        public string Password { get; set; } = "Passw0rd!x";

        public string Note { get; set; } = "a long free text that is not a secret at all, and is carried only to make the object a little bigger";
    }

    private readonly LogMasker _masker = new();
    private readonly Login _login = new();
    private readonly string _json = """{"email":"ali@example.com","password":"Passw0rd!x","token":"abc.def.ghi","items":[{"name":"a","cardNumber":"4111111111111111"},{"name":"b"}]}""";

    [Benchmark]
    public string MaskTextOfAJsonBody() => _masker.MaskText(_json);

    [Benchmark]
    public object MaskAnObject() => _masker.MaskObject(_login);

    [Benchmark]
    public string MaskTextThatIsNotJson() => _masker.MaskText("this is not json, it is a sentence of a log");
}

/// <summary>What every message pays on the wire: the envelope and the message as UTF-8 JSON, and back.</summary>
[MemoryDiagnoser]
public class EventSerializerBenchmarks
{
    public sealed class OrderCreated : IIntegrationEventMessage
    {
        public Guid OrderId { get; set; } = Guid.NewGuid();

        public string Customer { get; set; } = "acme";

        public decimal Total { get; set; } = 1234.56m;

        public List<string> Lines { get; set; } = ["a", "b", "c", "d"];
    }

    private readonly MessageEnvelope<OrderCreated> _envelope = new() { MessageId = Guid.NewGuid(), MessageTime = DateTime.UtcNow, CorrelationId = Guid.NewGuid().ToString("N"), Producer = "order-service", HopLevel = 1, Message = new OrderCreated() };
    private byte[] _body = [];

    [GlobalSetup]
    public void Setup() => _body = EventSerializer.Serialize(_envelope);

    [Benchmark]
    public byte[] Serialize() => EventSerializer.Serialize(_envelope);

    [Benchmark]
    public object DeserializeEnvelope() => EventSerializer.DeserializeEnvelope(_body, typeof(OrderCreated));

    [Benchmark]
    public ParentMessageEnvelope DeserializeParent() => EventSerializer.DeserializeParent(_body);
}

/// <summary>What every list endpoint pays: the sort a client sent, checked against the fields that may be sorted by, and the filter that is put together from the parts of a request.</summary>
[MemoryDiagnoser]
public class ListQueryBenchmarks
{
    public sealed record Product(string Name, int Price, string Sku);

    private static readonly SortFields<Product> s_fields = new SortFields<Product>().Add("name", product => product.Name).Add("price", product => product.Price).Add("sku", product => product.Sku);

    [Benchmark]
    public SortSpec<Product> ParseASort() => s_fields.Parse("price desc, name", "name");

    [Benchmark]
    public Expression<Func<Product, bool>> BuildAFilter()
        => new FilterBuilder<Product>().And(product => product.Price > 10).And(product => product.Name.StartsWith('a')).Or(product => product.Sku == "x").Build();
}

/// <summary>The helpers that run on every key, name and address.</summary>
[MemoryDiagnoser]
public class HelperBenchmarks
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "hsn-bench");
    private readonly IPAddress _public = IPAddress.Parse("93.184.216.34");
    private readonly IPAddress _private = IPAddress.Parse("10.1.2.3");

    [Benchmark]
    public string Normalize() => StringHelper.Normalize("  Çiğdem Köfte Ürünü  ");

    [Benchmark]
    public string SlugKey() => StringHelper.SlugKeyNormalize("Çanakkale Üniversitesi, Ltd. Şti.");

    [Benchmark]
    public bool IsPublicAddress() => PublicAddress.IsPublic(_public) && !PublicAddress.IsPublic(_private);

    [Benchmark]
    public string ResolveABlobPath() => BlobPaths.Resolve(_root, "invoices", "2026/10/invoice-0001.pdf");
}
