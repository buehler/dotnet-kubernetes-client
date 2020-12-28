using System;
using System.Reflection;
using k8s;
using k8s.Models;

namespace DotnetKubernetesClient.Entities
{
    public static class CustomEntityDefinitionExtensions
    {
        /// <summary>
        /// Create a custom entity definition.
        /// </summary>
        /// <param name="resource">The resource that is used as the type.</param>
        /// <returns>A <see cref="CustomEntityDefinition"/>.</returns>
        public static CustomEntityDefinition CreateResourceDefinition(
            this IKubernetesObject<V1ObjectMeta> resource) =>
            CreateResourceDefinition(resource.GetType());

        /// <summary>
        /// Create a custom entity definition.
        /// </summary>
        /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
        /// <returns>A <see cref="CustomEntityDefinition"/>.</returns>
        public static CustomEntityDefinition CreateResourceDefinition<TResource>()
            where TResource : IKubernetesObject<V1ObjectMeta> =>
            CreateResourceDefinition(typeof(TResource));

        /// <summary>
        /// Create a custom entity definition.
        /// </summary>
        /// <param name="resourceType">A type to construct the definition from.</param>
        /// <exception cref="ArgumentException">
        /// When the type of the resource does not contain a <see cref="KubernetesEntityAttribute"/>.
        /// </exception>
        /// <returns>A <see cref="CustomEntityDefinition"/>.</returns>
        public static CustomEntityDefinition CreateResourceDefinition(this Type resourceType)
        {
            var attribute = resourceType.GetCustomAttribute<KubernetesEntityAttribute>();
            if (attribute == null)
            {
                throw new ArgumentException($"The Type {resourceType} does not have the kubernetes entity attribute.");
            }

            var scopeAttribute = resourceType.GetCustomAttribute<EntityScopeAttribute>();
            var kind = string.IsNullOrWhiteSpace(attribute.Kind) ? resourceType.Name : attribute.Kind;

            return new CustomEntityDefinition(
                kind,
                $"{kind}List",
                attribute.Group,
                attribute.ApiVersion,
                kind.ToLower(),
                string.IsNullOrWhiteSpace(attribute.PluralName) ? $"{kind.ToLower()}s" : attribute.PluralName,
                scopeAttribute?.Scope ?? default);
        }
    }
}
