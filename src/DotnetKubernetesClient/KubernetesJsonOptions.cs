using System;
using System.Reflection;
using System.Text.Json;
using k8s;

#nullable enable

namespace DotnetKubernetesClient
{
    internal static class KubernetesJsonOptions
    {
        private static readonly Lazy<JsonSerializerOptions?> Lazy = new(GetJsonSerializerOptions);

        public static JsonSerializerOptions? DefaultOptions => Lazy.Value;

        private static JsonSerializerOptions? GetJsonSerializerOptions()
        {
            try
            {
                // Get the default (and private) KubernetesJson options.
                var fieldInfo = typeof(KubernetesJson).GetField("JsonSerializerOptions", BindingFlags.Static | BindingFlags.NonPublic);
                if (fieldInfo != null
                    && fieldInfo.GetValue(null) is JsonSerializerOptions options)
                {
                    return options;
                }
            }
            catch
            {
                // Ignored
            }

            return null;
        }
    }
}
