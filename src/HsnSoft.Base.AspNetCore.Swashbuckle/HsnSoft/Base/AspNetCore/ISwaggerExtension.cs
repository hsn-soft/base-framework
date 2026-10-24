using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;

namespace HsnSoft.Base.AspNetCore;

/// <summary>
/// Something that adds documents to the Swagger UI of the host, as the gateway package does with the documents of the services behind it. Register it as a
/// service; <see cref="SwaggerServiceCollectionExtensions.UseBaseSwagger"/> calls it, only where Swagger is served.
/// </summary>
public interface ISwaggerExtension
{
    /// <summary>The documents to show in the UI besides the host's own: where each is served from, and the name in the UI's list.</summary>
    IEnumerable<(string Url, string Name)> UiEndpoints { get; }

    /// <summary>Adds the middleware that serves those documents.</summary>
    void Use(IApplicationBuilder app);
}
