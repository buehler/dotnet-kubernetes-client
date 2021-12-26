using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace DotnetKubernetesClient.Serialization;

/// <summary>
/// This file is copied from https://github.com/kubernetes-client/csharp since it is internal.
/// </summary>
internal static class KubernetesJson
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new();

    static KubernetesJson()
    {
        JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        JsonSerializerOptions.Converters.Add(new Iso8601TimeSpanConverter());
        JsonSerializerOptions.Converters.Add(new KubernetesDateTimeConverter());
        JsonSerializerOptions.Converters.Add(new KubernetesDateTimeOffsetConverter());
        JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public static TValue? Deserialize<TValue>(string json) =>
        JsonSerializer.Deserialize<TValue>(json, JsonSerializerOptions);

    public static TValue? Deserialize<TValue>(Stream json) =>
        JsonSerializer.Deserialize<TValue>(json, JsonSerializerOptions);

    public static TValue? Deserialize<TValue>(JsonElement json) =>
        json.Deserialize<TValue>(JsonSerializerOptions);

    public static string Serialize(object value) => JsonSerializer.Serialize(value, JsonSerializerOptions);

    private class Iso8601TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            return XmlConvert.ToTimeSpan(str ?? string.Empty);
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            var iso8601TimeSpanString =
                XmlConvert.ToString(value); // XmlConvert for TimeSpan uses ISO8601, so delegate serialization to it
            writer.WriteStringValue(iso8601TimeSpanString);
        }
    }

    private class KubernetesDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        private const string SerializeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffffffK";
        private const string Iso8601Format = "yyyy'-'MM'-'dd'T'HH':'mm':'ssK";

        public override DateTimeOffset Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var str = reader.GetString();
            return DateTimeOffset.ParseExact(
                str,
                new[] { Iso8601Format, SerializeFormat },
                CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(SerializeFormat));
        }
    }

    private class KubernetesDateTimeConverter : JsonConverter<DateTime>
    {
        private static readonly JsonConverter<DateTimeOffset> UtcConverter = new KubernetesDateTimeOffsetConverter();

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return UtcConverter.Read(ref reader, typeToConvert, options).UtcDateTime;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            UtcConverter.Write(writer, value, options);
        }
    }
}
