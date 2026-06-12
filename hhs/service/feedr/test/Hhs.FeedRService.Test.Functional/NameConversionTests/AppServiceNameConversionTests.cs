using FluentAssertions;
using Hhs.FeedRService.Application;
using Hhs.FeedRService.Test.Shared.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.FeedRService.Test.Functional.NameConversionTests;

public class AppServiceNameConversionTests
{
    [Fact]
    public void AppService_TaskMethods_ShouldMethodNameEndAsync()
    {
        var appServices = ReflectionUtils.GetAppServices(typeof(ApplicationServiceBase).Assembly);

        var taskMethodNames = new List<string>();
        foreach (var appService in appServices)
        {
            taskMethodNames.AddRange(appService.GetMethods().Where
            (m => m.DeclaringType == appService // root class declaring
                  && (
                      (m.ReturnType.IsGenericType
                       && m.ReturnType.BaseType == typeof(Task)
                       && m.ReturnType.GenericTypeArguments.First() != typeof(IActionResult)
                      ) || (!m.ReturnType.IsGenericType && m.ReturnType == typeof(Task)))
            ).Select(x => x.Name).ToList());
        }

        if (taskMethodNames is not { Count: > 0 }) return;
        foreach (string methodName in taskMethodNames)
        {
            methodName.Should().EndWith("Async");
        }
    }

    [Fact]
    public void AppService_ValueReturnMethods_ShouldReturnModelNameEndDto()
    {
        var appServices = ReflectionUtils.GetAppServices(typeof(ApplicationServiceBase).Assembly);

        var methodReturnTypeNames = new List<string>();
        foreach (var appService in appServices)
        {
            methodReturnTypeNames.AddRange(appService.GetMethods().Where
            (m => m.DeclaringType == appService // root class declaring
                  && m.ReturnType is { IsInterface: false }
                  && ((!m.ReturnType.IsGenericType && m.ReturnType != typeof(Task)) || m.ReturnType.IsGenericType)
            ).Select(x => ReflectionUtils.GetGenericTypeName(x.ReturnType)).Distinct().ToList());
        }

        if (methodReturnTypeNames is not { Count: > 0 }) return;
        foreach (string typeName in methodReturnTypeNames.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            typeName.Should().EndWith("Dto");
        }
    }
}