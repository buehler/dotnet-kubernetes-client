# DotnetKubernetesClient

Enhanced version of the `KubernetesClient` of google.

The version that google provides is a generated one. They
generate code out of their formal definitions which is a
nice way of service a variety of languages.

However, there are many features in specific languages (such
as generics in C#) that would optimize such a library.

This library takes the generated one and just wrapps it
with a custom kubernetes client interface.

The interface takes now any resource (that has the `KubernetesObjectAttribute`)
and creates the needed calls for you.

## Example

```csharp
// Creates the client with the default config.
var client = new KubernetesClient();

// Get all namespaces in the cluster.
var namespaces = await client.List<V1Namespace>();

// All original methods are available through:
client.ApiClient;
```
