using HsnSoft.Base.Domain.Repositories;

namespace Edx.Mock.Center.Domain.Participants;

public interface IParticipantRepository : IRepository<Participant, Guid>
{
    Task<Participant?> FindByRegisterNumberAsync(string registerNumber, CancellationToken cancellationToken = default);
}
