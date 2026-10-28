namespace Edx.Shared.Contracts.Invoicing;

/// <summary>How an invoice reached the system (<c>CreatedChannelType</c>): through the API a customer calls, or through a page of the back office. All of them end up in the same queue and take the same steps.</summary>
public enum InvoiceChannelType
{
    /// <summary>The model (JSON or XML) sent to the API (REST, gRPC or WCF).</summary>
    ServiceDto = 1,

    /// <summary>A zip with a file sent to the API (REST, gRPC or WCF).</summary>
    ServiceUploadFile = 2,

    /// <summary>The model made in the create page of the back office.</summary>
    BackOfficeCreatePage = 3,

    /// <summary>A zip with a file sent from the upload page of the back office.</summary>
    BackOfficeUploadPage = 4,
}
