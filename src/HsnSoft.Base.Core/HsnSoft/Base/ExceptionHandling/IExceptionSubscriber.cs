using System.Threading.Tasks;
using JetBrains.Annotations;

namespace HsnSoft.Base.ExceptionHandling;

public interface IExceptionSubscriber
{
    Task HandleAsync([NotNull] ExceptionNotificationContext context);
}