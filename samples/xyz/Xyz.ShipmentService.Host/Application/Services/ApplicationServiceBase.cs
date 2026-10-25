using HsnSoft.Base.Application;
using Microsoft.Extensions.Localization;

namespace Xyz.ShipmentService.Application.Services;

/// <summary>
/// What every application service of the shipment service is: <c>Logger</c>, <c>CurrentUser</c> and <c>CurrentTenant</c> from the framework's <see cref="ApplicationService"/>, and <c>L</c>, which translates a text of a response
/// body into the language of the request (<c>L["OrderStatus:Pending"].Value</c>; the key comes back when no language has the text). The errors need no translating here: an exception names its text and the host translates it.
/// </summary>
public abstract class ApplicationServiceBase(IServiceProvider services) : ApplicationService(services)
{
    protected IStringLocalizer L => field ??= ServiceProvider.GetRequiredService<IStringLocalizer>();
}
