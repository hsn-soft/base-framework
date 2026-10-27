namespace Edx.IntakeService.Application.Archives;

/// <summary>What an archive is expected to hold.</summary>
public enum ArchiveKind
{
    /// <summary>Only XML files, each well formed.</summary>
    Xml,

    /// <summary>Only text, CSV or Excel files (the extensions of <c>Intake:TabularExtensions</c>).</summary>
    Tabular,
}
