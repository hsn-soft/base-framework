using HsnSoft.Base;

namespace Xyz.ShipmentService.Domain.CustomerDomain.Exceptions;

/// <summary>There is a customer with this user name already (a known error: the client is told, with the code <see cref="Code"/>).</summary>
public sealed class CustomerUserNameDuplicateException : BusinessException
{
    public const string Code = "customer_user_name_duplicate";

    public CustomerUserNameDuplicateException(string userName)
        : base(Code, $"A customer with the user name '{userName}' exists.") => WithKey(Code, userName);
}
