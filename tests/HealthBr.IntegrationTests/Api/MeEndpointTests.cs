using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using HealthBr.IntegrationTests.Persistence;

using Microsoft.IdentityModel.JsonWebTokens;

namespace HealthBr.IntegrationTests.Api;

/// <summary>
/// Covers <c>GET /api/v1/me</c> (task 2.2, spec 9): data of the logged user
/// for any role, always consistent with the token claims it authenticates
/// with.
/// </summary>
public sealed class MeEndpointTests(SqlServerFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task Me_WithDoctorToken_ShouldReturnDataConsistentWithToken()
    {
        var before = DateTime.UtcNow.AddSeconds(-5);
        var tenantId = await ProvisionTenantAsync(adminEmail: "doctor@clinic.com");
        var accessToken = await LoginForAccessTokenAsync("doctor@clinic.com", DefaultPassword);

        using var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/me", accessToken);
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = new JsonWebTokenHandler().ReadJsonWebToken(accessToken);

        Assert.Equal(token.GetClaim("sub")!.Value, body.GetProperty("id").GetString());
        Assert.Equal(token.GetClaim("email")!.Value, body.GetProperty("email").GetString());
        Assert.Equal(token.GetClaim("role")!.Value, body.GetProperty("role").GetString());
        Assert.Equal(token.GetClaim("tenant_id")!.Value, body.GetProperty("tenantId").GetString());
        Assert.Equal(tenantId.ToString(), body.GetProperty("tenantId").GetString());
        Assert.Equal("Dra. Ana Souza", body.GetProperty("name").GetString());
        Assert.InRange(body.GetProperty("createdAt").GetDateTime(), before, DateTime.UtcNow.AddSeconds(5));

        // The response never echoes secrets or persistence internals.
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
    }

    // Any role may call /me (spec 9): the receptionist is created through
    // POST /users by the tenant's doctor, then logs in with the credentials
    // just created.
    [Fact]
    public async Task Me_WithReceptionistToken_ShouldReturnDataConsistentWithToken()
    {
        await ProvisionTenantAsync(adminEmail: "doctor@clinic.com");
        var doctorToken = await LoginForAccessTokenAsync("doctor@clinic.com", DefaultPassword);

        using var creation = AuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/users",
            doctorToken,
            new { name = "Rita Reception", email = "reception@clinic.com", password = "Reception1" });
        Assert.Equal(HttpStatusCode.Created, (await Client.SendAsync(creation)).StatusCode);

        var receptionistToken = await LoginForAccessTokenAsync("reception@clinic.com", "Reception1");

        using var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/me", receptionistToken);
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Receptionist", body.GetProperty("role").GetString());
        Assert.Equal("reception@clinic.com", body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Me_WithoutToken_ShouldReturn401()
    {
        var response = await Client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithInvalidToken_ShouldReturn401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-jwt");
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
