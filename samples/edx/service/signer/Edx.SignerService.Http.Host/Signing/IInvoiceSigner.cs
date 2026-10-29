namespace Edx.SignerService.Http.Host.Signing;

/// <summary>Signs the UBL-TR invoice of an approved draft (XAdES-BES, enveloped, in the extension of the document). The real signer (a financial seal, a certificate) is this service; the sample has <see cref="FakeInvoiceSigner"/> in its place, and the host chooses.</summary>
public interface IInvoiceSigner
{
    /// <param name="xml">The unsigned invoice.</param>
    /// <param name="signerName">Who signs: the supplier of the invoice.</param>
    /// <returns>The invoice with the signature in it.</returns>
    byte[] Sign(byte[] xml, string signerName);
}
