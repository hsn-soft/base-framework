namespace Edx.InvoiceService.Application.Signing;

/// <summary>Signs the XML of an approved invoice. The real signer (a certificate, a time stamp) is a service of its own; the sample has <see cref="FakeInvoiceSigner"/> in its place, and the host chooses.</summary>
public interface IInvoiceSigner
{
    /// <summary>The XML with the signature in it.</summary>
    byte[] Sign(byte[] xml);
}
