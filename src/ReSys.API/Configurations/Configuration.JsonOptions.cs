using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using Serilog;

namespace ReSys.API.Configurations;

public static class JsonOptionsConfiguration
{
    public static IServiceCollection AddJsonConfig(this IServiceCollection services)
    {

        // Configure System.Text.Json for controllers
        services.AddControllers(configure: options =>
        {
            options.ModelBinderProviders.Insert(index: 0,
                item: new SnakeCaseQueryModelBinderProvider());
        })
            .AddJsonOptions(configure: options => ConfigureJsonOptions(options: options.JsonSerializerOptions));

        // Configure System.Text.Json for HTTP (used by TypedResults) Minimal API
        services.Configure<JsonOptions>(
            configureOptions: options => ConfigureJsonOptions(options: options.SerializerOptions));

        Log.Information(messageTemplate: "Register: JSON Configuration");
        return services;
    }

    private static void ConfigureJsonOptions(JsonSerializerOptions options)
    {
        options.IncludeFields = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.Converters.Add(item: new SystemTextJsonUtcDateTimeOffsetConverter());
        options.Converters.Add(item: new SystemTextJsonNullableDateTimeOffsetConverter());
    }

    /// <summary>
    /// Converter for DateTimeOffset that serializes to UTC with "Z" suffix.
    /// </summary>
    private sealed class SystemTextJsonUtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? dateString = reader.GetString();
                if (string.IsNullOrEmpty(value: dateString))
                    throw new JsonException(message: "Cannot convert empty string to DateTimeOffset.");

                if (DateTimeOffset.TryParse(input: dateString,
                        formatProvider: CultureInfo.InvariantCulture,
                        styles: DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        result: out DateTimeOffset dto))
                    return dto;

                throw new JsonException(message: $"Invalid date format: {dateString}");
            }

            throw new JsonException(message: $"Unexpected token parsing date. Expected String, got {reader.TokenType}.");
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            string isoString = value.ToUniversalTime().ToString(format: "yyyy-MM-dd'T'HH:mm:ss",
                formatProvider: CultureInfo.InvariantCulture) + "Z";
            writer.WriteStringValue(value: isoString);
        }
    }

    /// <summary>
    /// Converter for nullable DateTimeOffset that serializes to UTC with "Z" suffix or null.
    /// </summary>
    private sealed class SystemTextJsonNullableDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
    {
        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String)
            {
                string? dateString = reader.GetString();
                if (string.IsNullOrEmpty(value: dateString))
                    return null;

                if (DateTimeOffset.TryParse(input: dateString,
                        formatProvider: CultureInfo.InvariantCulture,
                        styles: DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        result: out DateTimeOffset dto))
                    return dto;

                throw new JsonException(message: $"Invalid date format: {dateString}");
            }

            throw new JsonException(message: $"Unexpected token parsing date. Expected String or Null, got {reader.TokenType}.");
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                string isoString = value.Value.ToUniversalTime().ToString(format: "yyyy-MM-dd'T'HH:mm:ss",
                    formatProvider: CultureInfo.InvariantCulture) + "Z";
                writer.WriteStringValue(value: isoString);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }

    private sealed class SnakeCaseQueryModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.IsComplexType)
            {
                return new SnakeCaseComplexTypeModelBinder();
            }
            return null;
        }
    }

    private sealed class SnakeCaseComplexTypeModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            Type modelType = bindingContext.ModelMetadata.ModelType;
            object? model = Activator.CreateInstance(type: modelType);

            if (model == null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

            foreach (PropertyInfo property in modelType.GetProperties())
            {
                string snakeCaseName = ConvertToSnakeCase(input: property.Name);
                ValueProviderResult value = bindingContext.ValueProvider.GetValue(key: snakeCaseName);

                if (value != ValueProviderResult.None && !string.IsNullOrEmpty(value: value.FirstValue))
                {
                    try
                    {
                        object convertedValue = Convert.ChangeType(value: value.FirstValue,
                            conversionType: property.PropertyType);
                        property.SetValue(obj: model,
                            value: convertedValue);
                    }
                    catch
                    {
                        // Handle conversion errors gracefully
                    }
                }
            }

            bindingContext.Result = ModelBindingResult.Success(model: model);
            return Task.CompletedTask;
        }

        private string ConvertToSnakeCase(string input)
        {
            return string.Concat(values: input.Select(selector: (x, i) => i > 0 && char.IsUpper(c: x) ? "_" + x : x.ToString())).ToLower();
        }
    }
}