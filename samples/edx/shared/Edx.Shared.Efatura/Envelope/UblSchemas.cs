using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Edx.Shared.Efatura.Envelope;

/// <summary>
/// The UBL-TR schemas of the invoice and of the application response (the <c>xsdrt</c> of the UBL-TR package, version 1.2.1): checks a document against them, which is the "XML schema check" of the center. The schemas are read from a directory that has the folders
/// <c>maindoc</c> (<c>UBL-Invoice-2.1.xsd</c>, <c>UBL-ApplicationResponse-2.1.xsd</c>) and <c>common</c>; the set is made once for each directory.
/// </summary>
public sealed class UblSchemas
{
    private static readonly ConcurrentDictionary<string, Lazy<UblSchemas>> s_cache = new();

    private readonly XmlSchemaSet _schemas;

    private UblSchemas(XmlSchemaSet schemas) => _schemas = schemas;

    /// <summary>The schemas that come with the library (the folder <c>Schemas</c> next to the application: the package's <c>xsdrt</c>, linked into the library's project).</summary>
    public static UblSchemas Default => Load(Path.Combine(AppContext.BaseDirectory, "Schemas"));

    /// <summary>The schemas in the directory (made on the first call).</summary>
    /// <exception cref="DirectoryNotFoundException">There is no such directory, or the schemas are not in it.</exception>
    public static UblSchemas Load(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);

        return s_cache.GetOrAdd(Path.GetFullPath(directory), path => new Lazy<UblSchemas>(() => Read(path))).Value;
    }

    /// <summary>What is wrong with the document, as the schema says (empty when it is right).</summary>
    public IReadOnlyList<string> Validate(XElement document)
    {
        ArgumentNullException.ThrowIfNull(document);

        List<string> problems = [];
        XmlReaderSettings settings = new() { ValidationType = ValidationType.Schema, Schemas = _schemas, DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        settings.ValidationEventHandler += (_, e) => problems.Add($"line {e.Exception?.LineNumber}: {e.Message}");
        using XmlReader reader = XmlReader.Create(document.CreateReader(), settings);
        while (reader.Read())
        {
            // reading validates
        }

        return problems;
    }

    private static UblSchemas Read(string directory)
    {
        string invoice = Path.Combine(directory, "maindoc", "UBL-Invoice-2.1.xsd");
        string response = Path.Combine(directory, "maindoc", "UBL-ApplicationResponse-2.1.xsd");
        if (!File.Exists(invoice) || !File.Exists(response))
        {
            throw new DirectoryNotFoundException($"The UBL-TR schemas are not in '{directory}' (maindoc/UBL-Invoice-2.1.xsd, maindoc/UBL-ApplicationResponse-2.1.xsd).");
        }

        XmlSchemaSet set = new() { XmlResolver = new XmlUrlResolver() };
        Add(set, invoice);
        Add(set, response);

        // the signature (xmldsig, XAdES) is declared by schemas the documents import by namespace only: they are given to the set with the rest of the common ones
        foreach (string common in Directory.EnumerateFiles(Path.Combine(directory, "common"), "*.xsd"))
        {
            Add(set, common);
        }

        set.Compile();
        return new UblSchemas(set);
    }

    // the schema files are the package's own, local and trusted; one of them (xmldsig) has a DTD for its entities, which has to be read
    private static void Add(XmlSchemaSet set, string path)
    {
        using XmlReader reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, XmlResolver = new XmlUrlResolver(), MaxCharactersFromEntities = 1_000_000 });
        set.Add(null, reader);
    }
}
