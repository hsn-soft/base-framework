using HsnSoft.Base.Logging.Masking;
using NLog;

namespace HsnSoft.Base.Logging.NLog.Tests;

public class NLogMessageTests
{
    private static readonly string[] s_letters = ["a", "b"];

    private sealed class Credentials
    {
        public string? User { get; set; } = "alice";

        [SensitiveData(SubstituteText = "***")]
        public string? Token { get; set; } = "abc";
    }

    private static LogEventInfo Event(string template, params object?[] parameters)
        => new(global::NLog.LogLevel.Info, "x", null, template, parameters);

    [Fact]
    public void ATemplate_IsFilledWithItsValues_AndTheEscapesAreUnescaped()
    {
        LogEventInfo logEvent = Event("{{literal}} {Name} is {Age:N1} {Unclosed", "alice", 3.5);

        Assert.Equal("{literal} alice is 3.5 {Unclosed", NLogMessage.Render(logEvent));
    }

    [Fact]
    public void APositionalTemplate_AndAMessageWithoutParameters_AreRenderedByNLog()
    {
        Assert.Equal("a b", NLogMessage.Render(Event("{0} {1}", "a", "b")));
        Assert.Equal("plain", NLogMessage.Render(new LogEventInfo(global::NLog.LogLevel.Info, "x", "plain")));
        Assert.Throws<ArgumentNullException>(() => NLogMessage.Render(null!));
    }

    [Fact]
    public void AnObject_IsWrittenAsMaskedJson_AndAScalarCapturedWithAtIsWrittenAsItIs()
    {
        LogEventInfo logEvent = Event("{@Credentials} {@Id} {@Missing}", new Credentials(), 7, null);

        Assert.Equal("""{"User":"alice","Token":"***"} 7 """, NLogMessage.Render(logEvent));
    }

    [Fact]
    public void TheValueOfAField_IsKeptCutOrWrittenAsJson()
    {
        Assert.Null(NLogMessage.FieldValue(null, 10));
        Assert.Equal(5, NLogMessage.FieldValue(5, 10));
        Assert.Equal("abcde", NLogMessage.FieldValue("abcdefghij", 5)!.ToString()![..5]);
        Assert.Equal("""{"User":"alice","Token":"***"}""", NLogMessage.FieldValue(new Credentials(), 100));
        Assert.Equal("http://x/", NLogMessage.FieldValue(new Uri("http://x"), 100));
        Assert.Equal("""["a","b"]""", NLogMessage.FieldValue(s_letters, 100));
        Assert.Equal("""{"a":1,"b":null,"c":true,"d":"2026-01-01T00:00:00"}""", NLogMessage.FieldValue(new Dictionary<string, object?> { ["a"] = 1, ["b"] = null, ["c"] = true, ["d"] = new DateTime(2026, 1, 1) }, 100));
        Assert.True(NLogMessage.IsStructured(new Credentials()));
        Assert.False(NLogMessage.IsStructured("text"));
        Assert.False(NLogMessage.IsStructured(null));
        Assert.False(NLogMessage.IsStructured(typeof(string)));
    }
}
