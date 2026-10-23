using HsnSoft.Base.Logging.Masking;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace HsnSoft.Base.Logging.Serilog.Tests;

/// <summary>What the policy makes of an object that is logged with the <c>@</c> operator.</summary>
public sealed class SensitiveDataDestructuringPolicyTests : IDisposable
{
    private readonly CaptureSink _sink = new();
    private readonly Logger _logger;

    public SensitiveDataDestructuringPolicyTests()
    {
        _logger = new LoggerConfiguration().Destructure.With<SensitiveDataDestructuringPolicy>().WriteTo.Sink(_sink).CreateLogger();
    }

    public void Dispose() => _logger.Dispose();

    public enum Kind
    {
        Basic,
        Premium,
    }

    public sealed class StringBag : IEnumerable<string>
    {
        private readonly List<string> _items = ["b1"];

        public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct Point
    {
        public int X { get; set; }

        public string? Label { get; set; }
    }

    public sealed class Node
    {
        public string WriteOnly
        {
            set => Touched = value;
        }

        public string? Name { get; set; }

        [SensitiveData(ShowLast = 2)]
        public string? Password { get; set; }

        public Node? Child { get; set; }

        public List<Node?> Children { get; set; } = [];

        public Dictionary<string, Node?> ByKey { get; set; } = [];

        [SensitiveData]
        public Dictionary<string, string> Secrets { get; set; } = [];

        [SensitiveData]
        public string[] Tokens { get; set; } = [];

        [SensitiveData]
        public IReadOnlyList<string> TokenList { get; set; } = [];

        [SensitiveData]
        public StringBag Bag { get; set; } = new();

        [SensitiveData]
        public List<int> PinList { get; set; } = [];

        [SensitiveData]
        public int Pin { get; set; }

        [SensitiveData]
        public object? Anything { get; set; } = "plain text in an object";

        [SensitiveData]
        public string? Missing { get; set; }

        public string Thrower => throw new InvalidOperationException("a getter that fails");

        public Node? Self { get; set; }

        public Guid Id { get; set; }

        public DateTime At { get; set; }

        public Kind Level { get; set; }

        public Point Location { get; set; }

        public int[] Numbers { get; set; } = [];

        public string? Touched { get; private set; }
    }

    private static Node Sample()
    {
        Node node = new()
        {
            Name = "root",
            Password = "hunter2",
            Child = new Node { Name = "child", Password = "abcdef" },
            Children = [new Node { Name = "c1", Password = "12345" }, null],
            ByKey = new Dictionary<string, Node?> { ["x"] = new Node { Name = "dx", Password = "pw" }, ["none"] = null },
            Secrets = new Dictionary<string, string> { ["key"] = "value" },
            Tokens = ["t1", "t2"],
            TokenList = ["l1"],
            PinList = [1, 2],
            Pin = 1234,
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            At = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            Level = Kind.Premium,
            Location = new Point { X = 3, Label = "here" },
            Numbers = [7, 8],
        };
        node.Self = node;
        node.WriteOnly = "written";
        return node;
    }

    private StructureValue LogObject(object value)
    {
        _logger.Information("{@Obj}", value);
        return Assert.IsType<StructureValue>(Assert.Single(_sink.Events).Properties["Obj"]);
    }

    private static LogEventPropertyValue Property(StructureValue structure, string name)
        => structure.Properties.Single(property => property.Name == name).Value;

    private static string? Text(LogEventPropertyValue value) => Assert.IsType<ScalarValue>(value).Value as string;

    [Fact]
    public void ObjectIsLoggedAsAStructureOfItsType_WithItsPlainPropertiesKept()
    {
        StructureValue logged = LogObject(Sample());

        Assert.Equal("Node", logged.TypeTag);
        Assert.Equal("root", Text(Property(logged, "Name")));
    }

    [Fact]
    public void Password_ShowsOnlyWhatTheAttributeAllows()
    {
        StructureValue logged = LogObject(Sample());

        Assert.Equal("*****r2", Text(Property(logged, "Password")));
    }

    [Fact]
    public void NestedObjects_AreMasked_InsideCollectionsAndDictionariesToo()
    {
        StructureValue logged = LogObject(Sample());

        StructureValue child = Assert.IsType<StructureValue>(Property(logged, "Child"));
        Assert.Equal("*****ef", Text(Property(child, "Password")));
        SequenceValue children = Assert.IsType<SequenceValue>(Property(logged, "Children"));
        StructureValue first = Assert.IsType<StructureValue>(children.Elements[0]);
        Assert.Equal("*****45", Text(Property(first, "Password")));
        Assert.Null(Assert.IsType<ScalarValue>(children.Elements[1]).Value);
        DictionaryValue byKey = Assert.IsType<DictionaryValue>(Property(logged, "ByKey"));
        StructureValue entry = Assert.IsType<StructureValue>(byKey.Elements.Single(pair => Equals(pair.Key.Value, "x")).Value);
        Assert.Equal("*****pw", Text(Property(entry, "Password")));
        Assert.Null(Assert.IsType<ScalarValue>(byKey.Elements.Single(pair => Equals(pair.Key.Value, "none")).Value).Value);
    }

    [Fact]
    public void SensitiveCollectionsOfText_AreMasked_ElementByElement()
    {
        StructureValue logged = LogObject(Sample());

        DictionaryValue secrets = Assert.IsType<DictionaryValue>(Property(logged, "Secrets"));
        Assert.Equal("*****", Text(secrets.Elements.Single().Value));
        Assert.All(Assert.IsType<SequenceValue>(Property(logged, "Tokens")).Elements, element => Assert.Equal("*****", Text(element)));
        Assert.All(Assert.IsType<SequenceValue>(Property(logged, "TokenList")).Elements, element => Assert.Equal("*****", Text(element)));
        Assert.All(Assert.IsType<SequenceValue>(Property(logged, "Bag")).Elements, element => Assert.Equal("*****", Text(element)));
    }

    [Fact]
    public void OtherSensitiveValues_AreHiddenAsATextWhole()
    {
        StructureValue logged = LogObject(Sample());

        Assert.Equal("*****", Text(Property(logged, "Pin")));
        Assert.Equal("*****", Text(Property(logged, "Anything")));
        Assert.Equal("*****", Text(Property(logged, "PinList")));
        Assert.Null(Assert.IsType<ScalarValue>(Property(logged, "Missing")).Value);
    }

    [Fact]
    public void SimpleValues_AreKept()
    {
        StructureValue logged = LogObject(Sample());

        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), Assert.IsType<ScalarValue>(Property(logged, "Id")).Value);
        Assert.Equal(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), Assert.IsType<ScalarValue>(Property(logged, "At")).Value);
        Assert.Equal(Kind.Premium, Assert.IsType<ScalarValue>(Property(logged, "Level")).Value);
        SequenceValue numbers = Assert.IsType<SequenceValue>(Property(logged, "Numbers"));
        Assert.Equal([7, 8], numbers.Elements.Select(element => ((ScalarValue)element).Value));
        StructureValue location = Assert.IsType<StructureValue>(Property(logged, "Location"));
        Assert.Equal("here", Text(Property(location, "Label")));
    }

    [Fact]
    public void PropertiesThatCannotBeRead_AreSkipped()
    {
        StructureValue logged = LogObject(Sample());

        Assert.DoesNotContain(logged.Properties, property => property.Name == "Thrower");
        Assert.DoesNotContain(logged.Properties, property => property.Name == "WriteOnly");
        Assert.Equal("written", Text(Property(logged, "Touched")));
    }

    [Fact]
    public void CircularReferences_AreMarked_NotFollowed()
    {
        StructureValue logged = LogObject(Sample());

        Assert.Equal("[CyclicRef:Node]", Text(Property(logged, "Self")));
    }

    [Fact]
    public void ObjectsThatAreNotObjects_AreHandledAtTheTopLevelToo()
    {
        _logger.Information("{@Obj}", new Dictionary<string, Node> { ["a"] = new() { Name = "n", Password = "pw" } });
        _logger.Information("{@Obj}", new List<Node> { new() { Name = "m", Password = "pw" } });
        _logger.Information("{@Obj}", 42);
        _logger.Information("{@Obj}", new Point { X = 1, Label = "p" });

        Assert.IsType<DictionaryValue>(_sink.Events[0].Properties["Obj"]);
        Assert.IsType<SequenceValue>(_sink.Events[1].Properties["Obj"]);
        Assert.Equal(42, Assert.IsType<ScalarValue>(_sink.Events[2].Properties["Obj"]).Value);
        Assert.Equal("Point", Assert.IsType<StructureValue>(_sink.Events[3].Properties["Obj"]).TypeTag);
    }

    [Fact]
    public void NullValue_IsAScalarNull()
    {
        _logger.Information("{@Obj}", (object?)null);

        Assert.Null(Assert.IsType<ScalarValue>(Assert.Single(_sink.Events).Properties["Obj"]).Value);
    }
}
