using Abc.IdentityService.Http.Host.Controllers.Base;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Masking;
using Microsoft.AspNetCore.Mvc;

namespace Abc.IdentityService.Http.Host.Controllers;

/// <summary>A class that is logged as a whole (<c>{@Order}</c>): what is marked <c>[SensitiveData]</c> in it is hidden in the log, by the class itself.</summary>
public sealed class ExampleOrder
{
    public string OrderNo { get; set; } = string.Empty;

    public decimal Total { get; set; }

    [SensitiveData]
    public string CardNumber { get; set; } = string.Empty;
}

/// <summary>
/// How a controller writes its application logs (they are the <c>AppLog</c>s of the service). <c>POST /api/log-examples/run</c> writes one of each (they are all written with <c>Logger</c>, the <c>ILogger</c> of the base controller), so they can be looked
/// at in Graylog (<c>Facility:LOG_EXAMPLE_*</c>). Delete this controller when you have seen them.
/// </summary>
[Route("api/log-examples")]
public sealed class LogExamplesController : BaseServiceController
{
    [HttpPost("run")]
    public IActionResult Run()
    {
        ExampleOrder order = new() { OrderNo = "A-1001", Total = 249.90m, CardNumber = "4111111111111111" };

        // 1. Plain text. BeginFacility names every log written inside the using: the Facility field in Graylog.
        using (Logger.BeginFacility("LOG_EXAMPLE_PLAIN"))
        {
            Logger.LogInformation("The order was received");
        }

        // 2. Text with arguments. Each {Name} becomes a field of its own (OrderNo, Total), so it can be searched: OrderNo:A-1001.
        using (Logger.BeginFacility("LOG_EXAMPLE_ARGS"))
        {
            Logger.LogInformation("Order {OrderNo} was checked, total {Total}", order.OrderNo, order.Total);
        }

        // 3. A class as the argument. {@Order} (with the @) writes the whole object as ONE field, Order, holding its JSON; MessageType is Structured.
        //    CardNumber is [SensitiveData] in the class: it is hidden in the log without anything written here.
        using (Logger.BeginFacility("LOG_EXAMPLE_OBJECT"))
        {
            Logger.LogInformation("Order was checked {@Order}", order);
        }

        // 4. Levels, and an exception: the exception is the first argument; its type, where it was thrown and why are fields of the log.
        using (Logger.BeginFacility("LOG_EXAMPLE_WARNING"))
        {
            Logger.LogWarning("Order {OrderNo} has no delivery address yet", order.OrderNo);
        }

        using (Logger.BeginFacility("LOG_EXAMPLE_ERROR"))
        {
            try
            {
                throw new InvalidOperationException("The payment service did not answer");
            }
            catch (InvalidOperationException exception)
            {
                Logger.LogError(exception, "Order {OrderNo} could not be paid", order.OrderNo);
            }
        }

        return Ok(new { facilities = "LOG_EXAMPLE_*" });
    }
}
