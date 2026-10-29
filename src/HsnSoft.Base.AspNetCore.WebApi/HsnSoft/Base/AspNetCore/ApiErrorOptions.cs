using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.AspNetCore;

/// <summary>What a failure's answer tells the client (section <c>ApiErrors</c>).</summary>
public sealed class ApiErrorOptions
{
    public const string SectionName = "ApiErrors";

    private static readonly string[] s_defaultHiddenIn = ["Production", "Staging", "stage", "uat"];

    /// <summary>
    /// The environments in which the answer of a failure of the service (a 500) says nothing but <c>INTERNAL_SERVER_ERROR</c> and the trace id: no message,
    /// no place in the code. Elsewhere (development, a developer's docker) it also says why and where, so the developer sees it without opening Graylog.
    /// When none are given: <c>Production</c>, <c>Staging</c>, <c>stage</c> and <c>uat</c> (a list from configuration adds to a default list, so the default is not a list).
    /// </summary>
    public List<string> HideDetailsIn { get; set; } = [];

    public bool HidesDetails(IHostEnvironment environment)
        => (HideDetailsIn.Count > 0 ? HideDetailsIn.AsEnumerable() : s_defaultHiddenIn).Any(environment.IsEnvironment);
}
