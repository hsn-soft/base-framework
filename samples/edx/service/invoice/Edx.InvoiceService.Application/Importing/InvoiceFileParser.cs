using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Edx.Shared.Contracts.Invoicing;
using ExcelDataReader;

namespace Edx.InvoiceService.Application.Importing;

/// <summary>
/// Reads the file of an invoice into the model of an invoice: CSV, TXT (<c>H|...</c> for the header and <c>L|...</c> for a line), a sheet (XLS or XLSX, the first one, the columns of <see cref="InvoiceTable"/>), or the model a client sent as JSON or XML. A file that does not follow its form is told what is
/// wrong (<see cref="InvoiceFileException"/>). An XML file is not read into the model: it is a UBL-TR invoice already, and <see cref="ParseUbl"/> reads it as a document.
/// </summary>
public static class InvoiceFileParser
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);

    static InvoiceFileParser() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <param name="source">The form of the file (not <see cref="InvoiceSourceType.Xml"/>: see <see cref="ParseUbl"/>).</param>
    /// <param name="content">Its bytes.</param>
    /// <param name="fileName">Its name: a model is JSON or XML by its extension.</param>
    /// <param name="invoiceId">The uuid of the invoice, which a table or a text file does not carry.</param>
    public static InvoiceSubmissionDto Parse(InvoiceSourceType source, byte[] content, string fileName, Guid invoiceId)
    {
        ArgumentNullException.ThrowIfNull(content);

        return source switch
        {
            InvoiceSourceType.Csv => InvoiceTable.Read(ReadCsv(Encoding.UTF8.GetString(content)), invoiceId),
            InvoiceSourceType.Txt => ParseText(Encoding.UTF8.GetString(content), invoiceId),
            InvoiceSourceType.Xls => InvoiceTable.Read(ReadSheet(content), invoiceId),
            InvoiceSourceType.Model => fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ? ParseModelXml(content) : ParseJson(content),
            _ => throw new InvoiceFileException($"The type {source} of file is not read into the model."),
        };
    }

    /// <summary>Reads an XML file as a UBL document (no DTD, no external entity); what the schema says of it is checked by the caller.</summary>
    public static XElement ParseUbl(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        try
        {
            using MemoryStream stream = new(content, writable: false);
            using XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            return XDocument.Load(reader).Root ?? throw new InvoiceFileException("The XML has no root element.");
        }
        catch (XmlException exception)
        {
            throw new InvoiceFileException($"The XML is not well formed: {exception.Message}");
        }
    }

    private static InvoiceSubmissionDto ParseModelXml(byte[] content)
    {
        try
        {
            return InvoiceXml.Read(content);
        }
        catch (InvoiceXmlException exception)
        {
            throw new InvoiceFileException(exception.Message);
        }
    }

    private static InvoiceSubmissionDto ParseJson(byte[] content)
    {
        try
        {
            return JsonSerializer.Deserialize<InvoiceSubmissionDto>(content, s_json) ?? throw new InvoiceFileException("The JSON holds no invoice.");
        }
        catch (JsonException exception)
        {
            throw new InvoiceFileException($"The JSON is not an invoice: {exception.Message}");
        }
    }

    // H|number|date|currency|customer|customerTaxId|taxOffice|district|city|country[|invoiceProfile], then L|description|quantity|unitCode|unitPrice|vatRate for each line; empty lines and lines that start with # are left
    private static InvoiceSubmissionDto ParseText(string text, Guid invoiceId)
    {
        string[]? header = null;
        List<InvoiceLineDto> lines = [];
        int number = 0;
        foreach (string raw in text.Split('\n'))
        {
            number++;
            string line = raw.Trim('\r', '\uFEFF');
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            string[] parts = [.. line.Split('|').Select(part => part.Trim())];
            switch (parts[0])
            {
                case "H" when header is not null:
                    throw new InvoiceFileException($"Line {number}: an invoice has one H line.");
                case "H":
                    header = parts.Length is 10 or 11 ? parts : throw new InvoiceFileException($"Line {number}: an H line has 10 parts, or 11 with the profile at the end (H|number|date|currency|customer|customerTaxId|taxOffice|district|city|country|invoiceProfile).");
                    break;
                case "L":
                    lines.Add(parts.Length == 6
                        ? InvoiceTable.Line(parts[1], parts[2], parts[3], parts[4], parts[5], number)
                        : throw new InvoiceFileException($"Line {number}: an L line has 6 parts (L|description|quantity|unitCode|unitPrice|vatRate)."));
                    break;
                default:
                    throw new InvoiceFileException($"Line {number}: a line starts with H or L, not '{parts[0]}'.");
            }
        }

        return header is null
            ? throw new InvoiceFileException("The file has no H line.")
            : InvoiceTable.Build(invoiceId, header[1], header[2], header[3], header[4], header[5], header[6], header[7], header[8], header[9], header.Length == 11 ? header[10] : string.Empty, lines);
    }

    // CSV as RFC 4180 says: commas, fields in quotes may hold commas, line breaks and a doubled quote
    private static List<IReadOnlyList<string>> ReadCsv(string text)
    {
        List<IReadOnlyList<string>> rows = [];
        List<string> row = [];
        StringBuilder field = new();
        bool quoted = false;
        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < text.Length && text[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else if (character == '"')
                {
                    quoted = false;
                }
                else
                {
                    field.Append(character);
                }

                continue;
            }

            switch (character)
            {
                case '"' when field.Length == 0:
                    quoted = true;
                    break;
                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = [];
                    break;
                case '﻿' when rows.Count == 0 && row.Count == 0 && field.Length == 0:
                    break;
                default:
                    field.Append(character);
                    break;
            }
        }

        if (quoted)
        {
            throw new InvoiceFileException("A field of the CSV starts with a quote and never ends.");
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }

        return rows;
    }

    // the first sheet, every cell as text (a date as yyyy-MM-dd, a number with a dot)
    private static List<IReadOnlyList<string>> ReadSheet(byte[] content)
    {
        try
        {
            using MemoryStream stream = new(content, writable: false);
            using IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream);
            List<IReadOnlyList<string>> rows = [];
            while (reader.Read())
            {
                string[] cells = new string[reader.FieldCount];
                for (int index = 0; index < cells.Length; index++)
                {
                    cells[index] = reader.GetValue(index) switch
                    {
                        null => string.Empty,
                        DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
                        var other => other.ToString() ?? string.Empty,
                    };
                }

                rows.Add(cells);
            }

            return rows;
        }
        catch (Exception exception) when (exception is ExcelDataReader.Exceptions.ExcelReaderException or InvalidDataException or IOException)
        {
            throw new InvoiceFileException($"The sheet cannot be read: {exception.Message}");
        }
    }
}
