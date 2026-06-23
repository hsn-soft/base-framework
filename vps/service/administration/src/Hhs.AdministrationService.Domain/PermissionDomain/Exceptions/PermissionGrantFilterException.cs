using HsnSoft.Base;
using Microsoft.Extensions.Localization;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Exceptions;

[Serializable]
internal sealed class PermissionGrantFilterException : BusinessException
{
    public PermissionGrantFilterException(IStringLocalizer localizer)
        : base(errorMessage: localizer[DomainErrorCodes.PermissionGrantFilter])
    {
        ErrorCode = DomainErrorCodes.PermissionGrantFilter.Split(':').LastOrDefault();
    }
}