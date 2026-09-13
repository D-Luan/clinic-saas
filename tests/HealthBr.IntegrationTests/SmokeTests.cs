using System.Reflection;

namespace HealthBr.IntegrationTests;

public class SmokeTests
{
    [Fact]
    public void ApiAssembly_IsLoadable()
    {
        var assembly = Assembly.Load("HealthBr.Api");

        Assert.NotNull(assembly);
    }
}
