using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;

namespace Hhs.EventManagerService.Domain.EventDomain.Exceptions;

[Serializable]
public sealed class FailedIntegrationEventStateException : BusinessException
{
    public FailedIntegrationEventStateException(string referenceCode)
        : base(errorMessage: DomainErrorCodes.FailedIntegrationEventStateError)
    {
        ErrorCode = DomainErrorCodes.FailedIntegrationEventStateError.Split(':').LastOrDefault();
        WithData(ValidationResourceKeys.ErrorReference, referenceCode);
    }
}