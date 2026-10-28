using Edx.Mock.Center.Domain.Participants;
using Microsoft.Extensions.Options;

namespace Edx.Mock.Center.Wcf.Host;

/// <summary>Keeps the participants in step with the settings at every start: the center itself, the customers of this integrator (Veriban, Bilgera) and a customer of another integrator, each with its aliases and the service that serves them.</summary>
public static class ParticipantSeeder
{
    public static async Task SeedParticipantsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        using IServiceScope scope = services.CreateScope();
        IParticipantRepository participants = scope.ServiceProvider.GetRequiredService<IParticipantRepository>();
        IConfiguration configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        CenterOptions center = scope.ServiceProvider.GetRequiredService<IOptions<CenterOptions>>().Value;
        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(ParticipantSeeder));

        // the center is a participant too: the post boxes answer it, from their address to its
        List<ParticipantSeed> seeds = [.. configuration.GetSection(ParticipantSeed.SectionName).Get<List<ParticipantSeed>>() ?? []];
        if (seeds.TrueForAll(seed => seed.RegisterNumber != center.RegisterNumber))
        {
            string own = configuration.GetValue<string>("Center:Endpoint") ?? "http://localhost:5700/EFaturaMerkez/services/EFatura/";
            seeds.Add(new ParticipantSeed { RegisterNumber = center.RegisterNumber, Title = center.Title, Aliases = [new AliasSeed { Name = center.Alias, Type = nameof(AliasType.SenderUnit), Endpoint = own }] });
        }

        foreach (ParticipantSeed seed in seeds)
        {
            List<ParticipantAlias> aliases = [.. seed.Aliases.Select(alias => ParticipantAlias.Create(alias.Name, Enum.Parse<AliasType>(alias.Type, ignoreCase: true), alias.Endpoint))];
            Participant? existing = await participants.FindByRegisterNumberAsync(seed.RegisterNumber, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                await participants.AddAsync(Participant.Create(Guid.NewGuid(), seed.RegisterNumber, seed.Title, aliases), cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Participant {RegisterNumber} ({Title}) was made with {AliasCount} aliases", seed.RegisterNumber, seed.Title, aliases.Count);
            }
            else
            {
                existing.Replace(seed.Title, aliases);
                await participants.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
