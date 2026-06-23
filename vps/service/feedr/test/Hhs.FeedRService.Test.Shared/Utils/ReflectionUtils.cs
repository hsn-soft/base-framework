using System.Reflection;
using HsnSoft.Base.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Hhs.FeedRService.Test.Shared.Utils;

public static class ReflectionUtils
{
    public static IEnumerable<Type> GetControllers(Assembly assembly)
    {
        var refType = typeof(Microsoft.AspNetCore.Mvc.ControllerBase);
        var results = assembly.GetTypes().Where
        (p => p != refType && refType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false }
        );

        return results.Where(p => p.BaseType != typeof(Microsoft.AspNetCore.Mvc.Controller));
    }

    public static IEnumerable<Type> GetAppServices(Assembly assembly)
    {
        var refType = typeof(IApplicationService);
        return assembly.GetTypes().Where
        (p => p != refType && refType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false }
        );
    }

    public static IEnumerable<Type> GetInternalEventMessages(Assembly assembly)
    {
        var refType = typeof(IIntegrationEventMessage);
        return assembly.GetTypes().Where
        (p => p != refType && refType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false }
        );
    }

    public static IEnumerable<Type> GetEventHandlers(Assembly assembly)
    {
        var refType = typeof(IIntegrationEventHandler);
        return assembly.GetTypes().Where
        (p => p != refType && refType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false }
        );
    }

    public static string GetGenericTypeName(Type type, bool skipInterface = true)
    {
        if (type == null) return string.Empty;

        if (skipInterface && (type is { IsInterface: true } || type is { IsClass: false })) return string.Empty;

        return !type.IsGenericType ? type.Name : GetGenericTypeName(type.GenericTypeArguments[0]);
    }
}