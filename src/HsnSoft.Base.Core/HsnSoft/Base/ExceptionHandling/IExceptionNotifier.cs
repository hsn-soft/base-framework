using System.Threading.Tasks;
using JetBrains.Annotations;

namespace HsnSoft.Base.ExceptionHandling;

public interface IExceptionNotifier
{
    Task NotifyAsync([NotNull] ExceptionNotificationContext context);
}