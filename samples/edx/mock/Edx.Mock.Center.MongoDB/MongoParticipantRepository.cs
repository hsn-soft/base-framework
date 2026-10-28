using Edx.Mock.Center.Domain.Participants;
using HsnSoft.Base.MongoDB;
using MongoDB.Driver;

namespace Edx.Mock.Center.MongoDB;

public sealed class MongoParticipantRepository(IMongoDatabase database, MongoEntityConcepts concepts, string collectionName)
    : MongoRepository<Participant, Guid>(database, concepts, collectionName), IParticipantRepository
{
    public Task<Participant?> FindByRegisterNumberAsync(string registerNumber, CancellationToken cancellationToken = default)
        => FindAsync(participant => participant.RegisterNumber == registerNumber, cancellationToken);
}
