using System.Reflection;
using FluentAssertions;
using Hhs.ContentService.Controllers.Base;
using Hhs.ContentService.Test.Shared.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.ContentService.Test.Functional.NameConversionTests;

public class ControllerNameConversionTests
{
    [Fact]
    public void Controller_Methods_ShouldReturnTask()
    {
        var controllers = ReflectionUtils.GetControllers(typeof(BaseServiceController).Assembly);

        var methods = new List<MethodInfo>();
        foreach (var controller in controllers)
        {
            methods.AddRange(controller.GetMethods().Where
            (m => m.DeclaringType == controller
            ).ToList());
        }

        var noTaskReturnMethods = methods.Where
        (m => (!m.ReturnType.IsGenericType && m.ReturnType != typeof(Task))
              || (m.ReturnType.IsGenericType && m.ReturnType.BaseType != typeof(Task))
        ).ToList();

        noTaskReturnMethods.Should().HaveCount(0);
    }

    [Fact]
    public void Controller_TaskMethods_ShouldMethodNameEndAsync()
    {
        var controllers = ReflectionUtils.GetControllers(typeof(BaseServiceController).Assembly);

        var taskMethodNames = new List<string>();
        foreach (var controller in controllers)
        {
            taskMethodNames.AddRange(controller.GetMethods().Where
            (m => m.DeclaringType == controller // root class declaring
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
    public void Controller_ValueReturnMethods_ShouldReturnModelNameEndDto()
    {
        var controllers = ReflectionUtils.GetControllers(typeof(BaseServiceController).Assembly);

        var methodReturnTypeNames = new List<string>();
        foreach (var controller in controllers)
        {
            methodReturnTypeNames.AddRange(controller.GetMethods().Where
            (m => m.DeclaringType == controller // root class declaring
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