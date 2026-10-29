namespace Edx.InvoiceService.Application.Importing;

/// <summary>An invoice file is not what its type says (a column is missing, a number cannot be read, ...): the message tells the sender what to correct.</summary>
public sealed class InvoiceFileException(string message) : Exception(message);
