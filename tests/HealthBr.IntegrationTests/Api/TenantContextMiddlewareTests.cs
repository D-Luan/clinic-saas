using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using HealthBr.IntegrationTests.Persistence;

using Microsoft.Extensions.Logging;

namespace HealthBr.IntegrationTests.Api;

public sealed class TenantContextMiddlewareTests(SqlServerFixture fixture) : ApiTestBase(fixture)
{
    // There is no authenticated endpoint yet (task 2.2); requests hit an
    // unmapped route (404) while the probe records what the pipeline did to
    // the tenant context.
    private const string UnmappedRoute = "/api/v1/definitely-not-mapped";

    private async Task<string> LoginAsync(string email)
    {
        var login = await Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = DefaultPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var payload = await login.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("accessToken missing from login response.");
    }

    [Fact]
    public async Task AuthenticatedRequest_ShouldPopulateTenantContextFromClaim()
    {
        var tenantId = Guid.NewGuid();
        await SeedUserAsync(tenantId, "doctor@clinic.com");
        var accessToken = await LoginAsync("doctor@clinic.com");

        using var request = new HttpRequestMessage(HttpMethod.Get, UnmappedRoute);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(tenantId, TenantProbe.TenantIdAfterPipeline);
    }

    [Fact]
    public async Task UnauthenticatedRequest_ShouldKeepTenantContextEmpty()
    {
        var response = await Client.GetAsync(UnmappedRoute);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(Guid.Empty, TenantProbe.TenantIdAfterPipeline);
    }

    [Fact]
    public async Task RequestWithTamperedToken_ShouldNotPopulateTenantContext()
    {
        var tenantId = Guid.NewGuid();
        await SeedUserAsync(tenantId, "doctor@clinic.com");
        var accessToken = await LoginAsync("doctor@clinic.com");

        // Break the signature: flip the last character of the token.
        var tampered = accessToken[..^1] + (accessToken[^1] == 'A' ? 'B' : 'A');

        ClearLogs();
        using var request = new HttpRequestMessage(HttpMethod.Get, UnmappedRoute);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tampered);
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(Guid.Empty, TenantProbe.TenantIdAfterPipeline);
        Assert.Contains(Logs, entry =>
            entry.Level == LogLevel.Warning
            && entry.Category == "HealthBr.Api.Auth"
            && entry.Message.Contains("rejected", StringComparison.OrdinalIgnoreCase));
    }
}
