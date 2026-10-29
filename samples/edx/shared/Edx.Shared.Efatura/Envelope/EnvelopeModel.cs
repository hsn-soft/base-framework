using System.Xml.Linq;

namespace Edx.Shared.Efatura.Envelope;

/// <summary>The kinds of envelope (the <c>Type</c> of the document identification of the header) and the document each carries: the table of the integration guide.</summary>
public static class EnvelopeTypes
{
    /// <summary>An invoice, from the sender's unit to the center, and from the center to the receiver's post box (the same envelope, as it came).</summary>
    public const string Sender = "SENDERENVELOPE";

    /// <summary>An application response (the receiver of a commercial invoice accepts, rejects or returns it), from the post box to the center and from the center to the sender's unit.</summary>
    public const string PostBox = "POSTBOXENVELOPE";

    /// <summary>A system response: the answer about another envelope (what became of it), from whoever processed it.</summary>
    public const string System = "SYSTEMENVELOPE";

    /// <summary>A user account (open or close), from a private integrator to the center.</summary>
    public const string User = "USERENVELOPE";
}

/// <summary>The kinds of document an envelope of an invoice holds (<c>ElementType</c>); the other kinds of the e-invoice family are not handled here.</summary>
public static class ElementTypes
{
    public const string Invoice = "INVOICE";
    public const string ApplicationResponse = "APPLICATIONRESPONSE";
}

/// <summary>One side of an envelope: the alias (<c>urn:mail:...gb</c> for a sender unit, <c>...pk</c> for a post box), the register number (VKN or TCKN) and the title.</summary>
public sealed record EnvelopeParty(string Alias, string RegisterNumber, string Title);

/// <summary>What the header of an envelope says (StandardBusinessDocumentHeader): header version 1.0, the standard <c>UBL-TR</c> and the type version <c>1.2</c> (UBL 2.1).</summary>
public sealed record EnvelopeHeader(
    string HeaderVersion,
    EnvelopeParty Sender,
    EnvelopeParty Receiver,
    string Standard,
    string TypeVersion,
    string InstanceIdentifier,
    string DocumentType,
    DateTime CreatedAt,
    string? ElementType,
    int ElementCount)
{
    public const string CurrentHeaderVersion = "1.0";
    public const string CurrentStandard = "UBL-TR";
    public const string CurrentTypeVersion = "1.2";

    /// <summary>A header of the current versions.</summary>
    public static EnvelopeHeader Create(EnvelopeParty sender, EnvelopeParty receiver, string documentType, string instanceIdentifier, DateTime createdAt, string? elementType, int elementCount)
        => new(CurrentHeaderVersion, sender, receiver, CurrentStandard, CurrentTypeVersion, instanceIdentifier, documentType, createdAt, elementType, elementCount);
}

/// <summary>An envelope read: its header and the documents in it (the root element of each).</summary>
public sealed record EnvelopeContent(EnvelopeHeader Header, IReadOnlyList<XElement> Elements);

/// <summary>An envelope is not what the center accepts: the code is the one of <see cref="EfaturaStatus"/> that says why.</summary>
public sealed class EnvelopeException(int code, string? detail = null) : Exception(detail ?? EfaturaStatus.NameOf(code))
{
    public int Code { get; } = code;
}
