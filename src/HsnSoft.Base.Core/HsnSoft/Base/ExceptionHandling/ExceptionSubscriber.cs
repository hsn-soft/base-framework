using System.Threading.Tasks;
using HsnSoft.Base.DependencyInjection;

namespace HsnSoft.Base.ExceptionHandling;

public abstract class ExceptionSubscriber : IExceptionSubscriber, ITransientDependency
{
    public abstract Task HandleAsync(ExceptionNotificationContext context);
}