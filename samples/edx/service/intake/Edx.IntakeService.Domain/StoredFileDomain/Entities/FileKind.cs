namespace Edx.IntakeService.Domain.StoredFileDomain.Entities;

/// <summary>What a received file is.</summary>
public enum FileKind
{
    /// <summary>A zip of XML files.</summary>
    XmlArchive = 0,

    /// <summary>A zip of text, CSV or Excel files.</summary>
    TabularArchive = 1,

    /// <summary>A request model of the shared API, kept as a JSON file.</summary>
    ModelJson = 2,

    /// <summary>A request model of the shared API, kept as an XML file.</summary>
    ModelXml = 3,
}
