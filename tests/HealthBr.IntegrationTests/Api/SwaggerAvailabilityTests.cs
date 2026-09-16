using System.Net;

using HealthBr.Api.Middlewares;
using HealthBr.IntegrationTests.Persistence;

namespace HealthBr.IntegrationTests.Api;

/// <summary>
/// Swagger mounts only in Development and Staging (spec 3.2); these tests
/// boot the real pipeline under "Development" and "Production" to pin both
/// sides of the gate.
/// </summary>
public sealed class SwaggerAvailabilityTests(SqlServerFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task Swagger_ShouldBeAvailableInDevelopment()
    {
        using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Documented deviation (spec 15.4): Swashbuckle's index.html boots
        // through an inline script, so /swagger responses outside Production
        // carry the relaxed CSP — every other header stays strict.
        Assert.Equal(SecurityHeadersMiddleware.SwaggerCsp, response.Headers.GetValues("Content-Security-Policy").Single());

        var document = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, document.StatusCode);
    }

    [Fact]
    public async Task Swagger_ShouldBeAbsentInProduction()
    {
        using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/index.html")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }

    [Fact]
    public async Task NonSwaggerResponses_ShouldKeepStrictCspInDevelopment()
    {
        using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SecurityHeadersMiddleware.StrictCsp, response.Headers.GetValues("Content-Security-Policy").Single());
    }
}
