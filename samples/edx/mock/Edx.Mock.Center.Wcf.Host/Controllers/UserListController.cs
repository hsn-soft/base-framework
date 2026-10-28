using System.IO.Compression;
using System.Text;
using System.Xml;
using Edx.Mock.Center.Domain.Participants;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Edx.Mock.Center.Wcf.Host.Controllers;

/// <summary>
/// The list of the registered users, as the center publishes it (<c>userList</c> of the e-Fatura center): a zip with one XML, <c>UserList</c>, of the taxpayers and their aliases. Integrators take it (the guide says at most once an hour) to know the address of the post box of whoever they send an
/// invoice to. There are two lists: the post boxes (<c>newUserPkListxml_full.zip</c>, what a sender needs) and the sender units (<c>newUserGbListxml_full.zip</c>).
/// </summary>
[ApiController]
public sealed class UserListController(IParticipantRepository participants) : ControllerBase
{
    [HttpGet("/newUserPkListxml_full.zip")]
    public Task<IActionResult> PostBoxesAsync(CancellationToken cancellationToken) => ListAsync(AliasType.PostBox, "newUserPkListxml_full", cancellationToken);

    [HttpGet("/newUserGbListxml_full.zip")]
    public Task<IActionResult> SenderUnitsAsync(CancellationToken cancellationToken) => ListAsync(AliasType.SenderUnit, "newUserGbListxml_full", cancellationToken);

    private async Task<IActionResult> ListAsync(AliasType type, string name, CancellationToken cancellationToken)
    {
        IReadOnlyList<Participant> all = await participants.GetListAsync(new ListQueryOptions<Participant>(), cancellationToken).ConfigureAwait(false);
        using MemoryStream xml = new();
        using (XmlWriter writer = XmlWriter.Create(xml, new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) }))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("UserList");
            foreach (Participant participant in all.Where(participant => participant.Aliases.Any(alias => alias.Type == type)))
            {
                writer.WriteStartElement("User");
                writer.WriteElementString("Identifier", participant.RegisterNumber);
                writer.WriteElementString("Title", participant.Title);
                writer.WriteElementString("Type", "OZEL");
                writer.WriteElementString("FirstCreationTime", "2026-01-01T00:00:00");
                writer.WriteElementString("AccountType", "OZELENTEGRASYON");
                writer.WriteStartElement("Documents");
                writer.WriteStartElement("Document");
                writer.WriteAttributeString("type", "Invoice");
                foreach (ParticipantAlias alias in participant.Aliases.Where(alias => alias.Type == type))
                {
                    writer.WriteStartElement("Alias");
                    writer.WriteElementString("Name", alias.Name);
                    writer.WriteElementString("CreationTime", "2026-01-01T00:00:00");
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        using MemoryStream zip = new();
        using (ZipArchive archive = new(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            using Stream entry = archive.CreateEntry(name + ".xml").Open();
            entry.Write(xml.ToArray());
        }

        return File(zip.ToArray(), "application/zip", name + ".zip");
    }
}
