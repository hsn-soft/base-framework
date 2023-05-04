using HsnSoft.Base.Swashbuckle;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection;

public static class BaseSwaggerGenOptionsExtensions
{
    public static void HideBaseEndpoints(this SwaggerGenOptions swaggerGenOptions)
    {
        swaggerGenOptions.DocumentFilter<BaseSwashbuckleDocumentFilter>();
    }
}