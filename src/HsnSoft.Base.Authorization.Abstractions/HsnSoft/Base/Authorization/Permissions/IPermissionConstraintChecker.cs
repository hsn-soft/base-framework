using System.Threading.Tasks;
using JetBrains.Annotations;

namespace HsnSoft.Base.Authorization.Permissions;

public interface IPermissionConstraintChecker
{
    [ItemCanBeNull] Task<string> GetValueAsync(string constraint);

    Task<decimal?> GetDecimalAsync(string constraint);

    Task<int?> GetIntAsync(string constraint);

    Task<bool?> GetBooleanAsync(string constraint);
}