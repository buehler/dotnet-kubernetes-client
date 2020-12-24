﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DotnetKubernetesClient.Entities;
using DotnetKubernetesClient.LabelSelectors;
using DotnetKubernetesClient.Serialization;
using k8s;
using k8s.Models;
using Microsoft.Rest;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace DotnetKubernetesClient
{
    /// <inheritdoc />
    public class KubernetesClient : IKubernetesClient
    {
        private const string DownwardApiNamespaceFile = "/var/run/secrets/kubernetes.io/serviceaccount/namespace";
        private const string DefaultNamespace = "default";

        private readonly KubernetesClientConfiguration _clientConfig;

        public KubernetesClient()
            : this(KubernetesClientConfiguration.BuildDefaultConfig())
        {
        }

        public KubernetesClient(KubernetesClientConfiguration clientConfig)
        {
            _clientConfig = clientConfig;
            ApiClient = new Kubernetes(clientConfig, new ClientUrlFixer())
            {
                SerializationSettings =
                {
                    Formatting = Formatting.Indented,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    ContractResolver = new KubernetesNamingConvention(),
                    Converters = new List<JsonConverter>
                    {
                        new StringEnumConverter { CamelCaseText = true },
                        new Iso8601TimeSpanConverter(),
                    },
                    DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffffffK",
                },
                DeserializationSettings =
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    ContractResolver = new KubernetesNamingConvention(),
                    Converters = new List<JsonConverter>
                    {
                        new StringEnumConverter { CamelCaseText = true },
                        new Iso8601TimeSpanConverter(),
                    },
                    DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffffffK",
                },
            };
        }

        public KubernetesClient(IKubernetes apiClient, KubernetesClientConfiguration clientConfig)
        {
            _clientConfig = clientConfig;
            ApiClient = apiClient;
        }

        /// <inheritdoc />
        public IKubernetes ApiClient { get; }

        /// <inheritdoc />
        public Task<string> GetCurrentNamespace(string downwardApiEnvName = "POD_NAMESPACE")
        {
            string result = DefaultNamespace;

            if (_clientConfig.Namespace != null)
            {
                result = _clientConfig.Namespace;
            }

            if (Environment.GetEnvironmentVariable(downwardApiEnvName) != null)
            {
                result = Environment.GetEnvironmentVariable(downwardApiEnvName) ?? string.Empty;
            }

            if (File.Exists(DownwardApiNamespaceFile))
            {
                var ns = File.ReadAllText(DownwardApiNamespaceFile);
                result = ns.Trim();
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<VersionInfo> GetServerVersion() => ApiClient.GetCodeAsync();

        /// <inheritdoc />
        public async Task<TResource?> Get<TResource>(
            string name,
            string? @namespace = null)
            where TResource : class, IKubernetesObject<V1ObjectMeta>
        {
            var crd = CustomEntityDefinitionExtensions.CreateResourceDefinition<TResource>();
            try
            {
                var result = await (string.IsNullOrWhiteSpace(@namespace)
                    ? ApiClient.GetClusterCustomObjectAsync(crd.Group, crd.Version, crd.Plural, name)
                    : ApiClient.GetNamespacedCustomObjectAsync(
                        crd.Group,
                        crd.Version,
                        @namespace,
                        crd.Plural,
                        name)) as JObject;
                return result?.ToObject<TResource>();
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<IList<TResource>> List<TResource>(
            string? @namespace = null,
            string? labelSelector = null)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var crd = CustomEntityDefinitionExtensions.CreateResourceDefinition<TResource>();
            var result = await (string.IsNullOrWhiteSpace(@namespace)
                ? ApiClient.ListClusterCustomObjectAsync(
                    crd.Group,
                    crd.Version,
                    crd.Plural,
                    labelSelector: labelSelector)
                : ApiClient.ListNamespacedCustomObjectAsync(
                    crd.Group,
                    crd.Version,
                    @namespace,
                    crd.Plural,
                    labelSelector: labelSelector)) as JObject;

            var resources = result?.ToObject<EntityList<TResource>>();
            if (resources == null)
            {
                throw new ArgumentException("Could not parse result");
            }

            return resources.Items;
        }

        /// <inheritdoc />
        public Task<IList<TResource>> List<TResource>(
            string? @namespace = null,
            params ILabelSelector[] labelSelectors)
            where TResource : IKubernetesObject<V1ObjectMeta> =>
            List<TResource>(@namespace, string.Join(",", labelSelectors.Select(l => l.ToExpression())));

        /// <inheritdoc />
        public async Task<TResource> Save<TResource>(TResource resource)
            where TResource : class, IKubernetesObject<V1ObjectMeta>
        {
            var serverResource = await Get<TResource>(resource.Metadata.Name, resource.Metadata.NamespaceProperty);
            if (serverResource == null)
            {
                return await Create(resource);
            }

            resource.Metadata.Uid = serverResource.Metadata.Uid;
            resource.Metadata.ResourceVersion = serverResource.Metadata.ResourceVersion;

            return await Update(resource);
        }

        /// <inheritdoc />
        public async Task<TResource> Create<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var crd = resource.CreateResourceDefinition();
            var result = await (string.IsNullOrWhiteSpace(resource.Metadata.NamespaceProperty)
                ? ApiClient.CreateClusterCustomObjectAsync(
                    resource,
                    crd.Group,
                    crd.Version,
                    crd.Plural)
                : ApiClient.CreateNamespacedCustomObjectAsync(
                    resource,
                    crd.Group,
                    crd.Version,
                    resource.Metadata.NamespaceProperty,
                    crd.Plural)) as JObject;

            if (result?.ToObject(resource.GetType()) is TResource parsed)
            {
                resource.Metadata.ResourceVersion = parsed.Metadata.ResourceVersion;
                return parsed;
            }

            throw new ArgumentException("Could not parse result");
        }

        /// <inheritdoc />
        public async Task<TResource> Update<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var crd = resource.CreateResourceDefinition();
            var result = await (string.IsNullOrWhiteSpace(resource.Metadata.NamespaceProperty)
                ? ApiClient.ReplaceClusterCustomObjectAsync(
                    resource,
                    crd.Group,
                    crd.Version,
                    crd.Plural,
                    resource.Metadata.Name)
                : ApiClient.ReplaceNamespacedCustomObjectAsync(
                    resource,
                    crd.Group,
                    crd.Version,
                    resource.Metadata.NamespaceProperty,
                    crd.Plural,
                    resource.Metadata.Name)) as JObject;

            if (result?.ToObject(resource.GetType()) is TResource parsed)
            {
                resource.Metadata.ResourceVersion = parsed.Metadata.ResourceVersion;
                return parsed;
            }

            throw new ArgumentException("Could not parse result");
        }

        /// <inheritdoc />
        public async Task UpdateStatus<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>, IStatus<object>
        {
            var crd = resource.CreateResourceDefinition();
            var result = await (string.IsNullOrWhiteSpace(resource.Metadata.NamespaceProperty)
                ? ApiClient.ReplaceClusterCustomObjectStatusAsync(
                    resource,
                    crd.Group,
                    crd.Version,
                    crd.Plural,
                    resource.Metadata.Name)
                : ApiClient.ReplaceNamespacedCustomObjectStatusAsync(
                    resource,
                    crd.Group,
                    crd.Version,
                    resource.Metadata.NamespaceProperty,
                    crd.Plural,
                    resource.Metadata.Name)) as JObject;

            if (result?.ToObject(resource.GetType()) is IKubernetesObject<V1ObjectMeta> parsed)
            {
                resource.Metadata.ResourceVersion = parsed.Metadata.ResourceVersion;
                return;
            }

            throw new ArgumentException("Could not parse result");
        }

        /// <inheritdoc />
        public Task Delete<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta> => Delete<TResource>(
            resource.Metadata.Name,
            resource.Metadata.NamespaceProperty);

        /// <inheritdoc />
        public Task Delete<TResource>(IEnumerable<TResource> resources)
            where TResource : IKubernetesObject<V1ObjectMeta> =>
            Task.WhenAll(resources.Select(Delete));

        /// <inheritdoc />
        public Task Delete<TResource>(params TResource[] resources)
            where TResource : IKubernetesObject<V1ObjectMeta> =>
            Task.WhenAll(resources.Select(Delete));

        /// <inheritdoc />
        public async Task Delete<TResource>(string name, string? @namespace = null)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var crd = CustomEntityDefinitionExtensions.CreateResourceDefinition<TResource>();
            await (string.IsNullOrWhiteSpace(@namespace)
                ? ApiClient.DeleteClusterCustomObjectAsync(
                    crd.Group,
                    crd.Version,
                    crd.Plural,
                    name)
                : ApiClient.DeleteNamespacedCustomObjectAsync(
                    crd.Group,
                    crd.Version,
                    @namespace,
                    crd.Plural,
                    name));
        }

        /// <inheritdoc />
        public Task<Watcher<TResource>> Watch<TResource>(
            TimeSpan timeout,
            Action<WatchEventType, TResource> onEvent,
            Action<Exception>? onError = null,
            Action? onClose = null,
            string? @namespace = null,
            CancellationToken cancellationToken = default,
            params ILabelSelector[] labelSelectors)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Watch<TResource>(
                timeout,
                onEvent,
                onError,
                onClose,
                @namespace,
                cancellationToken,
                string.Join(",", labelSelectors.Select(l => l.ToExpression())));

        /// <inheritdoc />
        public Task<Watcher<TResource>> Watch<TResource>(
        TimeSpan timeout,
        Action<WatchEventType, TResource> onEvent,
        Action<Exception>? onError = null,
        Action? onClose = null,
        string? @namespace = null,
        CancellationToken cancellationToken = default,
        string? labelSelector = null)
        where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var crd = CustomEntityDefinitionExtensions.CreateResourceDefinition<TResource>();
            var result = string.IsNullOrWhiteSpace(@namespace)
                ? ApiClient.ListClusterCustomObjectWithHttpMessagesAsync(
                    crd.Group,
                    crd.Version,
                    crd.Plural,
                    labelSelector: labelSelector,
                    timeoutSeconds: (int)timeout.TotalSeconds,
                    watch: true,
                    cancellationToken: cancellationToken)
                : ApiClient.ListNamespacedCustomObjectWithHttpMessagesAsync(
                    crd.Group,
                    crd.Version,
                    @namespace,
                    crd.Plural,
                    labelSelector: labelSelector,
                    timeoutSeconds: (int)timeout.TotalSeconds,
                    watch: true,
                    cancellationToken: cancellationToken);

            return Task.FromResult(
                result.Watch(
                    onEvent,
                    onError,
                    onClose));
        }
    }
}
