using FluentAssertions;
using Xunit;

namespace DotnetKubernetesClient.Test
{
    public class KubernetesJsonOptionsTest
    {
        [Fact]
        public void DefaultOptions_should_not_return_null()
        {
            var result = KubernetesJsonOptions.DefaultOptions;

            result.Should().NotBe(null);
        }
    }
}
