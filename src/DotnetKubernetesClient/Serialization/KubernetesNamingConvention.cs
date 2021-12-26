using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DotnetKubernetesClient.Serialization;

internal class KubernetesNamingConvention : CamelCasePropertyNamesContractResolver
{
    private readonly IDictionary<string, string> _rename = new Dictionary<string, string>
    {
        { "namespaceProperty", "namespace" },
        { "enumProperty", "enum" },
        { "objectProperty", "object" },
    };

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        var result = _rename.FirstOrDefault(
            p =>
                string.Equals(property.PropertyName, p.Key, StringComparison.InvariantCultureIgnoreCase));

        if (result.Key != default)
        {
            property.PropertyName = result.Value;
        }

        return property;
    }
}
