using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using Edx.Integration.GibInvoice.Application.Options;
using Edx.Integration.GibInvoice.Domain.DirectoryDomain.Entities;
using Edx.Integration.GibInvoice.Domain.DirectoryDomain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Edx.Integration.GibInvoice.Application.Services;

/// <summary>
/// The users of the e-invoice system the service may send to. An invoice is addressed to the post box of its customer, and the only way to know it is the list the center publishes (a zip with <c>UserList</c>, the taxpayers and the aliases of their post boxes: the ones that are in force have no
/// <c>DeletionTime</c>). The service reads the list from the center (<see cref="SyncAsync"/>, as often as the settings say) and answers from its copy.
/// </summary>
public sealed class UserDirectory(IExchangeUserRepository users, IOptions<GibCenterOptions> options, TimeProvider time, ILogger<UserDirectory> logger)
{
    /// <summary>The user with this register number, when the list has it.</summary>
    public Task<ExchangeUser?> FindAsync(string registerNumber, CancellationToken cancellationToken = default) => users.FindByRegisterNumberAsync(registerNumber, cancellationToken);

    /// <summary>Reads the list of the center and keeps it. Returns how many users it has.</summary>
    public async Task<int> SyncAsync(CancellationToken cancellationToken = default)
    {
        using HttpClient client = new() { Timeout = TimeSpan.FromMinutes(2) };
        byte[] zip = await client.GetByteArrayAsync(new Uri(options.Value.UserListUrl), cancellationToken).ConfigureAwait(false);
        DateTimeOffset now = time.GetUtcNow();

        List<ExchangeUser> list = [];
        using MemoryStream stream = new(zip, writable: false);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);
        foreach (ZipArchiveEntry entry in archive.Entries.Where(entry => entry.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
        {
            await using Stream xml = entry.Open();
            using XmlReader reader = XmlReader.Create(xml, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            while (reader.Read())
            {
                if (reader is { NodeType: XmlNodeType.Element, LocalName: "User" } && XNode.ReadFrom(reader) is XElement user && Read(user, now) is { } read)
                {
                    list.Add(read);
                }
            }
        }

        await users.ReplaceAllAsync(list, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("The list of the registered users was read: {UserCount} users with a post box", list.Count);
        return list.Count;
    }

    // a user with the aliases of its post boxes that are in force (the newest first); one with none is left out
    private static ExchangeUser? Read(XElement user, DateTimeOffset now)
    {
        string? identifier = user.Element("Identifier")?.Value.Trim();
        if (string.IsNullOrEmpty(identifier))
        {
            return null;
        }

        string[] aliases =
        [
            .. user.Descendants("Document").Where(document => (string?)document.Attribute("type") == "Invoice")
                .SelectMany(document => document.Elements("Alias"))
                .Where(alias => alias.Element("DeletionTime") is null && !string.IsNullOrWhiteSpace(alias.Element("Name")?.Value))
                .OrderByDescending(alias => alias.Element("CreationTime")?.Value, StringComparer.Ordinal)
                .Select(alias => alias.Element("Name")!.Value.Trim()),
        ];
        return aliases.Length == 0 ? null : ExchangeUser.Of(identifier, user.Element("Title")?.Value.Trim() is { Length: > 0 } title ? title : identifier, aliases, now);
    }
}
