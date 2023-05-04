using System;
using System.Threading.Tasks;

namespace HsnSoft.Base.SecurityLog;

public interface ISecurityLogManager
{
    Task SaveAsync(Action<SecurityLogInfo> saveAction = null);
}
