using System.Text.Json;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus.Tests;

public class ParentMessageEnvelopeTests
{
    [Fact]
    public void Header_IsFoundWithoutRegardToCase()
    {
        ParentMessageEnvelope envelope = new();
        envelope.SetHeader("UserId", "u1");

        Assert.Equal("u1", envelope.GetHeader("userid"));
        Assert.Equal("u1", envelope.GetHeader("USERID"));
    }

    [Fact]
    public void MissingHeader_IsNull_AlsoWithoutAnyHeaders()
    {
        ParentMessageEnvelope envelope = new();

        Assert.Null(envelope.GetHeader("UserId"));

        envelope.SetHeader("Other", "x");
        Assert.Null(envelope.GetHeader("UserId"));
    }

    [Fact]
    public void NullHeader_ReadsAsNull_AndNumberAsItsText()
    {
        ParentMessageEnvelope envelope = new()
        {
            Headers = new Dictionary<string, JsonElement>
            {
                ["Empty"] = JsonSerializer.SerializeToElement<string?>(null),
                ["Number"] = JsonSerializer.SerializeToElement(42),
            },
        };

        Assert.Null(envelope.GetHeader("Empty"));
        Assert.Equal("42", envelope.GetHeader("Number"));
    }

    [Fact]
    public void SettingAHeaderToNull_RemovesIt()
    {
        ParentMessageEnvelope envelope = new();
        envelope.SetHeader("UserId", "u1");

        envelope.SetHeader("UserId", null);
        envelope.SetHeader("NeverThere", null);

        Assert.Null(envelope.GetHeader("UserId"));
    }

    [Fact]
    public void SettingAHeaderToNull_OnAnEnvelopeWithoutHeaders_DoesNothing()
    {
        ParentMessageEnvelope envelope = new();

        envelope.SetHeader("UserId", null);

        Assert.Null(envelope.Headers);
    }

    [Fact]
    public void HeaderNeedsAName()
    {
        Assert.Throws<ArgumentException>(() => new ParentMessageEnvelope().SetHeader(" ", "x"));
    }

    [Fact]
    public void HeadersToInherit_LeaveOutWhatBelongsToTheEnvelope()
    {
        ParentMessageEnvelope envelope = new();
        envelope.SetHeader("UserId", "u1");
        envelope.SetHeader("message", "not a header");
        envelope.SetHeader("parentmessageid", "not a header either");

        string[] names = [.. envelope.HeadersToInherit().Select(header => header.Key)];

        Assert.Equal(["UserId"], names);
        Assert.Empty(new ParentMessageEnvelope().HeadersToInherit());
    }
}
