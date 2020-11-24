using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using k8s;
using Moq;
using Xunit;

namespace DotnetKubernetesClient.Test
{
    public class KubernetesClientTest
    {
        private readonly IKubernetesClient _client;
        private readonly Mock<IKubernetes> _mock = new();

        public KubernetesClientTest()
        {
            _client = new KubernetesClient(_mock.Object, KubernetesClientConfiguration.BuildDefaultConfig());
        }

        [Fact]
        public async  Task Should_Return_Namespace()
        {
            var ns = await _client.GetCurrentNamespace();
            ns.Should().Be("default");
        }

        [Fact]
        public async  Task Should_Call_GetCode()
        {
            _mock.Setup(o => o.GetCodeWithHttpMessagesAsync(null, default));
            await _client.GetServerVersion();
            _mock.Verify(o => o.GetCodeWithHttpMessagesAsync(null, default), Times.Once);
        }
    }
}
