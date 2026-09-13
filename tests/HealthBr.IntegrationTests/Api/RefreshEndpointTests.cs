using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using HealthBr.Application.Common.Security;
using HealthBr.IntegrationTests.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthBr.IntegrationTests.Api;

public sealed class RefreshEndpointTests(SqlServerFixture fixture) : ApiTestBase(fixture)
{
    private async Task<string> LoginAndExtractCookieAsync()
    {
        var login = await Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "doctor@clinic.com", password = DefaultPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return ExtractRefreshToken(login);
    }

    [Fact]
    public async Task Refresh_WithValidCookie_ShouldRotateTokensAndRevokePrevious()
    {
        var tenantId = Guid.NewGuid();
        var user = await SeedUserAsync(tenantId, "doctor@clinic.com");
        var firstCookie = await LoginAndExtractCookieAsync();
        var firstHash = RefreshTokenGenerator.Hash(firstCookie);

        var response = await PostRefreshAsync(firstCookie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(payload.GetProperty("accessToken").GetString()));

        var secondCookie = ExtractRefreshToken(response);
        Assert.NotEqual(firstCookie, secondCookie);

        await using (var context = CreateContext())
        {
            var revoked = Assert.Single(
                await context.RefreshTokens.IgnoreQueryFilters()
                    .Where(token => token.Token == firstHash)
                    .ToListAsync());
            Assert.NotNull(revoked.RevokedAt);

            var issued = Assert.Single(
                await context.RefreshTokens.IgnoreQueryFilters()
                    .Where(token => token.UserId == user.Id && token.Token != firstHash)
                    .ToListAsync());
            Assert.Null(issued.RevokedAt);
            Assert.True(issued.ExpiresAt > DateTime.UtcNow);
        }

        // The revoked cookie must not authenticate again (spec 5.1 rotation).
        var reuse = await PostRefreshAsync(firstCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_ShouldReturn401()
    {
        var tenantId = Guid.NewGuid();
        var user = await SeedUserAsync(tenantId, "doctor@clinic.com");
        var rawToken = "revoked-raw-token-value";
        await SeedRefreshTokenAsync(
            user,
            RefreshTokenGenerator.Hash(rawToken),
            revokedAt: DateTime.UtcNow.AddMinutes(-5));

        var response = await PostRefreshAsync(rawToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(Logs, entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("revoked", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ShouldReturn401()
    {
        var tenantId = Guid.NewGuid();
        var user = await SeedUserAsync(tenantId, "doctor@clinic.com");
        var rawToken = "expired-raw-token-value";
        await SeedRefreshTokenAsync(
            user,
            RefreshTokenGenerator.Hash(rawToken),
            expiresAt: DateTime.UtcNow.AddDays(-1));

        var response = await PostRefreshAsync(rawToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(Logs, entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("expired", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ShouldReturn401()
    {
        var tenantId = Guid.NewGuid();
        await SeedUserAsync(tenantId, "doctor@clinic.com");

        var response = await PostRefreshAsync("totally-unknown-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(Logs, entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("not recognized", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ShouldReturn401()
    {
        var response = await PostRefreshAsync(cookieValue: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
