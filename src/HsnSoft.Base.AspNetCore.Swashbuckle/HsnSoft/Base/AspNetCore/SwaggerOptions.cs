using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.AspNetCore;

/// <summary>Where the Swagger document and its UI are served (section <c>Swagger</c>).</summary>
public sealed class SwaggerOptions
{
    public const string SectionName = "Swagger";

    private static readonly string[] s_defaultDisabledIn = ["Production", "Staging", "stage", "uat"];

    /// <summary>
    /// Serves the document and the UI (<see langword="true"/>) or not (<see langword="false"/>), whatever the environment is. When it is not set:
    /// served, except in <see cref="DisabledIn"/>.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// The environments in which nothing is served unless <see cref="Enabled"/> says so. When none are given: <c>Production</c>, <c>Staging</c>, <c>stage</c> and
    /// <c>uat</c>, what the services have always treated as running for real (a list from configuration adds to a default list, so the default is not a list).
    /// </summary>
    public List<string> DisabledIn { get; set; } = [];

    /// <summary>The path of the UI; the document is at <c>/swagger/{name}/swagger.json</c>.</summary>
    public string RoutePrefix { get; set; } = "swagger";

    public bool IsEnabled(IHostEnvironment environment)
        => Enabled ?? !(DisabledIn.Count > 0 ? DisabledIn.AsEnumerable() : s_defaultDisabledIn).Any(environment.IsEnvironment);
}
