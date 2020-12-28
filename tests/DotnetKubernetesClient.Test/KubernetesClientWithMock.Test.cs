using System.Threading.Tasks;
using FluentAssertions;
using k8s;
using Moq;
using Xunit;

namespace DotnetKubernetesClient.Test
{
    public class KubernetesClientWithMockTest
    {
        private readonly IKubernetesClient _client;
        private readonly Mock<IKubernetes> _mock = new();

        public KubernetesClientWithMockTest()
        {
            _client = new KubernetesClient(_mock.Object, KubernetesClientConfiguration.BuildDefaultConfig());
        }

        [Fact]
        public async  Task Should_Return_Namespace()
        {
            var ns = await _client.GetCurrentNamespace();
            ns.Should().Be("default");
        }
    }
}
