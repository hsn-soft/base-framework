using Edx.Shared.Efatura.Envelope;

namespace Edx.Shared.Efatura.Server;

/// <summary>What a service of the exchange checks of a <c>sendDocument</c> call before it queues the envelope (Ek-3): the file name is the uuid of the envelope and a <c>.zip</c> (2006), the MD5 is the zip's (2000), and nothing is missing (2005).</summary>
public static class SendDocumentRules
{
    /// <returns>The uuid of the envelope (the file name without its extension).</returns>
    /// <exception cref="EFaturaFaultException">The call is refused: 2005 a parameter is missing, 2006 the name, 2000 the hash.</exception>
    public static string Check(string? fileName, byte[]? content, string? hash)
    {
        if (content is not { Length: > 0 } || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(hash))
        {
            throw new EFaturaFaultException(EfaturaStatus.SystemFault, $"{EfaturaStatus.NameOf(EfaturaStatus.SystemFault)}: the file name, the zip and the hash are required.");
        }

        if (!Guid.TryParse(Path.GetFileNameWithoutExtension(fileName), out Guid parsed) || parsed == Guid.Empty)
        {
            throw new EFaturaFaultException(EfaturaStatus.InvalidEnvelopeName, $"{EfaturaStatus.NameOf(EfaturaStatus.InvalidEnvelopeName)}: the file name is the uuid of the envelope (XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX.zip).");
        }

        if (!string.Equals(Path.GetExtension(fileName), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new EFaturaFaultException(EfaturaStatus.InvalidEnvelopeName, $"{EfaturaStatus.NameOf(EfaturaStatus.InvalidEnvelopeName)}: an envelope is a .zip.");
        }

        if (!string.Equals(Hashing.Md5Hex(content), hash, StringComparison.OrdinalIgnoreCase))
        {
            throw new EFaturaFaultException(EfaturaStatus.HashesDiffer);
        }

        return Path.GetFileNameWithoutExtension(fileName);
    }

    /// <summary>The uuid a <c>getApplicationResponse</c> call asks about, normalized.</summary>
    /// <exception cref="EFaturaFaultException">It is not a uuid (2006).</exception>
    public static string CheckInstance(string? instanceIdentifier)
        => Guid.TryParse(instanceIdentifier, out Guid parsed) && parsed != Guid.Empty
            ? parsed.ToString()
            : throw new EFaturaFaultException(EfaturaStatus.InvalidEnvelopeName, $"{EfaturaStatus.NameOf(EfaturaStatus.InvalidEnvelopeName)}: instanceIdentifier is the uuid of the envelope.");
}
