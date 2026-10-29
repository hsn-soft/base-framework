using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;

namespace Edx.Mock.Center.Domain.Participants;

/// <summary>What an alias is for: a sender unit (<c>gb</c>, the address an envelope is sent from) or a post box (<c>pk</c>, the address an envelope is delivered to).</summary>
public enum AliasType
{
    SenderUnit = 1,
    PostBox = 2,
}

/// <summary>One address of a participant (<c>urn:mail:veriban_pk@veriban.example</c>) and where the center delivers what is sent to it.</summary>
public sealed class ParticipantAlias
{
    // for the database driver
    private ParticipantAlias()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public AliasType Type { get; private set; }

    /// <summary>The service of the integrator that serves this alias (the post box of an integrator is the one service of all its customers): the center sends the envelopes of the alias here.</summary>
    public string Endpoint { get; private set; } = string.Empty;

    public static ParticipantAlias Create(string name, AliasType type, string endpoint)
        => new()
        {
            Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), 200),
            Type = type,
            Endpoint = Guard.NotNullOrWhiteSpace(endpoint, nameof(Endpoint), 500),
        };
}

/// <summary>
/// A taxpayer registered with the center (by its VKN or TCKN) and the aliases it has: the center knows a participant only by this list, which is how it decides whether a sender may send from an alias, and where an envelope for a receiver goes. A participant of another integrator is
/// here too: its post box alias points at the other integrator's service.
/// </summary>
public sealed class Participant : Entity<Guid>
{
    // for the database driver
    private Participant()
    {
    }

    public string RegisterNumber { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public IReadOnlyList<ParticipantAlias> Aliases { get; private set; } = [];

    public static Participant Create(Guid id, string registerNumber, string title, IEnumerable<ParticipantAlias> aliases)
        => new()
        {
            Id = id,
            RegisterNumber = Guard.NotNullOrWhiteSpace(registerNumber, nameof(RegisterNumber), 11),
            Title = Guard.NotNullOrWhiteSpace(title, nameof(Title), 200),
            Aliases = [.. aliases],
        };

    /// <summary>The title and the aliases are the ones of the settings (the seeder keeps them in step).</summary>
    public void Replace(string title, IEnumerable<ParticipantAlias> aliases)
    {
        Title = Guard.NotNullOrWhiteSpace(title, nameof(Title), 200);
        Aliases = [.. aliases];
    }

    public ParticipantAlias? FindAlias(string name, AliasType type)
        => Aliases.FirstOrDefault(alias => alias.Type == type && string.Equals(alias.Name, name, StringComparison.Ordinal));

    /// <summary>The alias by its name, of any type.</summary>
    public ParticipantAlias? FindAlias(string name)
        => Aliases.FirstOrDefault(alias => string.Equals(alias.Name, name, StringComparison.Ordinal));
}
