using System;
using System.Text.Encodings.Web;
using HsnSoft.Base.Json.SystemTextJson.JsonConverters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Json.SystemTextJson;

public class BaseSystemTextJsonSerializerOptionsSetup : IConfigureOptions<BaseSystemTextJsonSerializerOptions>
{
    protected IServiceProvider ServiceProvider { get; }

    public BaseSystemTextJsonSerializerOptionsSetup(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public void Configure(BaseSystemTextJsonSerializerOptions options)
    {
        options.JsonSerializerOptions.Converters.Add(ServiceProvider.GetRequiredService<BaseDateTimeConverter>());
        options.JsonSerializerOptions.Converters.Add(ServiceProvider.GetRequiredService<BaseNullableDateTimeConverter>());

        options.JsonSerializerOptions.Converters.Add(new BaseStringToEnumFactory());
        options.JsonSerializerOptions.Converters.Add(new BaseStringToBooleanConverter());

        options.JsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());

        // If the user hasn't explicitly configured the encoder, use the less strict encoder that does not encode all non-ASCII characters.
        options.JsonSerializerOptions.Encoder ??= JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    }
}