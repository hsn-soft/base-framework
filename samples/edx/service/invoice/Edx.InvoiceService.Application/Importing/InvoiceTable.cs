using System.Globalization;
using Edx.Shared.Contracts.Invoicing;
using Edx.Shared.Efatura.Ubl;

namespace Edx.InvoiceService.Application.Importing;

/// <summary>
/// An invoice as a table: a header row, then a row for each line of the invoice, which says the header of the invoice again (a CSV or a sheet has no other way to keep two things in one table). The columns are
/// <c>invoiceNumber, issueDate, currency, customerName, customerTaxId, customerTaxOffice, customerDistrict, customerCity, customerCountry, description, quantity, unitCode, unitPrice, vatRate</c> (any order, any case; the tax office, the country and the unit may be left out). The header of the invoice is the one of
/// the first row; a later row may leave it empty, and may not say another one. A date is <c>yyyy-MM-dd</c>, a decimal has a dot. There is no supplier: the invoice is the tenant's.
/// </summary>
internal static class InvoiceTable
{
    private static readonly string[] s_required = ["invoiceNumber", "issueDate", "currency", "customerName", "customerTaxId", "customerDistrict", "customerCity", "description", "quantity", "unitPrice", "vatRate"];
    private static readonly string[] s_optional = ["customerTaxOffice", "customerCountry", "unitCode", "invoiceProfile"];
    private static readonly string[] s_headerNames = ["invoiceNumber", "issueDate", "currency", "customerName", "customerTaxId", "customerTaxOffice", "customerDistrict", "customerCity", "customerCountry", "invoiceProfile"];

    public static InvoiceSubmissionDto Read(IReadOnlyList<IReadOnlyList<string>> rows, Guid invoiceId)
    {
        if (rows.Count == 0)
        {
            throw new InvoiceFileException("The file is empty: it needs a header row and a row for each line of the invoice.");
        }

        Dictionary<string, int> known = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < rows[0].Count; index++)
        {
            string name = rows[0][index].Trim();
            if (name.Length > 0)
            {
                known[name] = index;
            }
        }

        string[] missing = [.. s_required.Where(name => !known.ContainsKey(name))];
        if (missing.Length > 0)
        {
            throw new InvoiceFileException($"The header row has no column {string.Join(", ", missing)}. The columns are {string.Join(", ", s_required.Concat(s_optional))}.");
        }

        string Cell(IReadOnlyList<string> row, string name) => known.TryGetValue(name, out int index) && index < row.Count ? row[index].Trim() : string.Empty;

        List<string[]> data = [.. rows.Skip(1).Where(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell))).Select(row => (string[])[.. row])];
        if (data.Count == 0)
        {
            throw new InvoiceFileException("The file has a header row and no line: an invoice needs at least one.");
        }

        Dictionary<string, string> header = s_headerNames.ToDictionary(name => name, name => Cell(data[0], name), StringComparer.Ordinal);
        List<InvoiceLineDto> lines = [];
        for (int index = 0; index < data.Count; index++)
        {
            int rowNumber = index + 2;
            foreach (string name in s_headerNames)
            {
                string value = Cell(data[index], name);
                if (index > 0 && value.Length > 0 && !string.Equals(value, header[name], StringComparison.Ordinal))
                {
                    throw new InvoiceFileException($"Row {rowNumber}: '{name}' is '{value}', and the first row said '{header[name]}': an invoice has one header.");
                }
            }

            lines.Add(Line(Cell(data[index], "description"), Cell(data[index], "quantity"), Cell(data[index], "unitCode"), Cell(data[index], "unitPrice"), Cell(data[index], "vatRate"), rowNumber));
        }

        return Build(invoiceId, header["invoiceNumber"], header["issueDate"], header["currency"], header["customerName"], header["customerTaxId"], header["customerTaxOffice"], header["customerDistrict"], header["customerCity"], header["customerCountry"], header["invoiceProfile"], lines);
    }

    public static InvoiceLineDto Line(string description, string quantity, string unitCode, string unitPrice, string vatRate, int row)
        => new()
        {
            Description = description,
            Quantity = Number(quantity, "quantity", row),
            UnitCode = unitCode.Length == 0 ? null : unitCode,
            UnitPrice = Number(unitPrice, "unitPrice", row),
            VatRate = Number(vatRate, "vatRate", row),
        };

    public static InvoiceSubmissionDto Build(Guid invoiceId, string number, string issueDate, string currency, string customerName, string customerTaxId, string customerTaxOffice, string customerDistrict, string customerCity, string customerCountry, string invoiceProfile, List<InvoiceLineDto> lines)
        => new()
        {
            InvoiceId = invoiceId,
            InvoiceNumber = number,
            IssueDate = Date(issueDate),
            Currency = currency,
            CustomerName = customerName,
            CustomerTaxId = customerTaxId,
            CustomerTaxOffice = customerTaxOffice.Length == 0 ? null : customerTaxOffice,
            CustomerDistrict = customerDistrict,
            CustomerCity = customerCity,
            CustomerCountry = customerCountry.Length == 0 ? null : customerCountry,
            InvoiceProfile = invoiceProfile.Length == 0 ? UblInvoiceBuilder.ProfileId : invoiceProfile.ToUpperInvariant(),
            Lines = lines,
        };

    public static DateTime Date(string text)
        => DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)
            ? date
            : throw new InvoiceFileException($"The date '{text}' is not a date in the form yyyy-MM-dd.");

    public static decimal Number(string text, string column, int row)
        => decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value)
            ? value
            : throw new InvoiceFileException($"Row {row}: '{column}' is '{text}', which is not a number (a decimal has a dot).");
}
