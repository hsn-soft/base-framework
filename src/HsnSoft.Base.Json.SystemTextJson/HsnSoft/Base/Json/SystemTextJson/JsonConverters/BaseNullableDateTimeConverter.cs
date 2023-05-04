using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Timing;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Json.SystemTextJson.JsonConverters;

public class BaseNullableDateTimeConverter : JsonConverter<DateTime?>, ITransientDependency
{
    private readonly IClock _clock;
    private readonly BaseJsonOptions _options;

    public BaseNullableDateTimeConverter(IClock clock, IOptions<BaseJsonOptions> baseJsonOptions)
    {
        _clock = clock;
        _options = baseJsonOptions.Value;
    }

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!_options.DefaultDateTimeFormat.IsNullOrWhiteSpace())
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (DateTime.TryParseExact(s, _options.DefaultDateTimeFormat, CultureInfo.CurrentUICulture, DateTimeStyles.None, out var d1))
                {
                    return _clock.Normalize(d1);
                }

                throw new JsonException($"'{s}' can't parse to DateTime({_options.DefaultDateTimeFormat})!");
            }

            throw new JsonException("Reader's TokenType is not String!");
        }

        if (reader.TryGetDateTime(out var d2))
        {
            return _clock.Normalize(d2);
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            if (_options.DefaultDateTimeFormat.IsNullOrWhiteSpace())
            {
                writer.WriteStringValue(_clock.Normalize(value.Value));
            }
            else
            {
                writer.WriteStringValue(_clock.Normalize(value.Value).ToString(_options.DefaultDateTimeFormat, CultureInfo.CurrentUICulture));
            }
        }
    }
}