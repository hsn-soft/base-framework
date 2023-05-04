using System.Threading.Tasks;

namespace HsnSoft.Base.SecurityLog;

public interface ISecurityLogStore
{
    Task SaveAsync(SecurityLogInfo securityLogInfo);
}
