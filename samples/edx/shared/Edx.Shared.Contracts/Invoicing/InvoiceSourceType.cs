namespace Edx.Shared.Contracts.Invoicing;

/// <summary>What an invoice came as: the form it had when it reached the system (<c>CreatedSourceType</c>). A file comes in a zip (the zip is only for the network), and the type is the one of the single file inside it.</summary>
public enum InvoiceSourceType
{
    /// <summary>The request model of the shared API (JSON or XML), made in a client or in the create page of the back office.</summary>
    Model = 1,

    /// <summary>An XML file in the data contract of the model (<c>urn:edx:intake:invoice:v1</c>).</summary>
    Xml = 2,

    /// <summary>A text file: a header line (<c>H|...</c>) and a line (<c>L|...</c>) for each item, with <c>|</c> between the fields.</summary>
    Txt = 3,

    /// <summary>A CSV file: a header row and a row for each item.</summary>
    Csv = 4,

    /// <summary>An Excel workbook (<c>.xls</c> or <c>.xlsx</c>): the first sheet, a header row and a row for each item.</summary>
    Xls = 5,
}
