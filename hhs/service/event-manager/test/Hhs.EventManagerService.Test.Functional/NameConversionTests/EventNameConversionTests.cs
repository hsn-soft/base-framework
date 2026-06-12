using FluentAssertions;
using Hhs.EventManagerService.Test.Shared.Utils;
using HsnSoft.Base.EventBus;

namespace Hhs.EventManagerService.Test.Functional.NameConversionTests;

public class EventNameConversionTests
{
    // [Fact]
    // public void EventMessage_Name_ShouldEndEto()
    // {
    //     var refAssembly = typeof(SynchAllPermissionToCacheDbEto).Assembly;
    //     var eventMessages = ReflectionUtils.GetInternalEventMessages(refAssembly);
    //
    //     foreach (var eventMessage in eventMessages)
    //     {
    //         eventMessage.Name.Should().EndWith("Eto");
    //     }
    // }

    [Fact]
    public void EventHandler_Name_ShouldEndEtoHandler()
    {
        var refAssembly = typeof(EventHandlersAssemblyMarker).Assembly;
        var eventHandlers = ReflectionUtils.GetEventHandlers(refAssembly);

        foreach (var eventHandler in eventHandlers)
        {
            eventHandler.Name.Should().EndWith("EtoHandler");
        }
    }

    [Fact]
    public void EventHandler_Name_ShouldEventNameHandler()
    {
        var refAssembly = typeof(EventHandlersAssemblyMarker).Assembly;
        var eventHandlers = ReflectionUtils.GetEventHandlers(refAssembly);

        foreach (var eventHandler in eventHandlers)
        {
            var interfaces = eventHandler.GetInterfaces();

            interfaces.Should().NotBeNull();
            interfaces.Should().HaveCountGreaterThan(0);

            var targetInterface = interfaces.FirstOrDefault(i =>
                i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)
            );

            targetInterface.Should().NotBeNull();

            var eventType = targetInterface.GetGenericArguments()[0];

            eventType.Should().NotBeNull();
            eventHandler.Name.Should().EndWith($"{eventType.Name}Handler");
        }
    }
}