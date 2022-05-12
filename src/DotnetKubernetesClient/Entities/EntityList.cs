using System.Collections.Generic;
using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace DotnetKubernetesClient.Entities;

public class EntityList<T> : KubernetesObject
    where T : IKubernetesObject<V1ObjectMeta>
{
    [JsonPropertyName("metadata")]
    public V1ListMeta Metadata { get; set; } = new ();

    [JsonPropertyName("items")]
    public IList<T> Items { get; set; } = new List<T>();
}
