using Edx.Shared.Efatura.Ubl;

namespace Edx.SignerService.Http.Host.Signing;

/// <summary>A signer that signs nothing: the invoice gets a signature of the right shape (<see cref="UblSignature.AddFake"/>) with made-up values, so the invoice is what the center wants to see and what the pipeline does with a signed invoice can be seen end to end.</summary>
public sealed class FakeInvoiceSigner(TimeProvider time) : IInvoiceSigner
{
    public byte[] Sign(byte[] xml, string signerName)
    {
        ArgumentNullException.ThrowIfNull(xml);

        return UblXml.ToBytes(UblSignature.AddFake(UblXml.Parse(xml), time.GetLocalNow().DateTime, signerName));
    }
}
