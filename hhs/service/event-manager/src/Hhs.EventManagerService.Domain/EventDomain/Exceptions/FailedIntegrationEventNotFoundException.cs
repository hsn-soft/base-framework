using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;

namespace Hhs.EventManagerService.Domain.EventDomain.Exceptions;

[Serializable]
internal sealed class FailedIntegrationEventNotFoundException : BusinessException
{
    public FailedIntegrationEventNotFoundException(string referenceCode)
        : base(errorMessage: DomainErrorCodes.FailedIntegrationEventNotFound)
    {
        ErrorCode = DomainErrorCodes.FailedIntegrationEventNotFound.Split(':').LastOrDefault();
        WithData(ValidationResourceKeys.ErrorReference, referenceCode);
    }
}