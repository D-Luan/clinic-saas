using System.Reflection;

namespace HealthBr.UnitTests;

public class SmokeTests
{
    [Fact]
    public void DomainAssembly_IsLoadable()
    {
        var assembly = Assembly.Load("HealthBr.Domain");

        Assert.NotNull(assembly);
    }
}
