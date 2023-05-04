using System.Threading.Tasks;

namespace HsnSoft.Base.ExceptionHandling;

public class NullExceptionNotifier : IExceptionNotifier
{
    private NullExceptionNotifier()
    {
    }

    public static NullExceptionNotifier Instance { get; } = new NullExceptionNotifier();

    public Task NotifyAsync(ExceptionNotificationContext context)
    {
        return Task.CompletedTask;
    }
}