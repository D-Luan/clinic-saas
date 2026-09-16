using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using HealthBr.Application.Common.Security;
using HealthBr.IntegrationTests.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthBr.IntegrationTests.Api;

/// <summary>
/// Covers <c>POST /api/v1/auth/logout</c> (task 2.2, spec 5.1/9/15.5):
/// idempotent revocation of the cookie's refresh token, cookie deletion with
/// the same Path, and 401 on any later refresh attempt (task 1.3 regression).
/// </summary>
public sealed class LogoutEndpointTests(SqlServerFixture fixture) : ApiTestBase(fixture)
{
    private async Task<HttpResponseMessage> LogoutAsync(string accessToken, string? cookieValue)
    {
        ClearLogs();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        if (cookieValue is not null)
        {
            request.Headers.TryAddWithoutValidation(
                "Cookie",
                $"{AuthConstants.RefreshTokenCookieName}={cookieValue}");
        }

        return await Client.SendAsync(request);
    }

    [Fact]
    public async Task Logout_WithValidCookie_ShouldRevokeTokenClearCookieAndReturn204()
    {
        await ProvisionTenantAsync(adminEmail: "doctor@clinic.com");
        var (accessToken, refreshCookie) = await LoginForSessionAsync("doctor@clinic.com", DefaultPassword);
        var tokenHash = RefreshTokenGenerator.Hash(refreshCookie);

        var response = await LogoutAsync(accessToken, refreshCookie);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // The cookie is deleted with the same Path it was set with (spec 15.5).
        var setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("refresh_token=", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=Thu, 01 Jan 1970", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", setCookie, StringComparison.OrdinalIgnoreCase);

        // The presented token is revoked (RevokedAt set, UTC).
        await using (var context = CreateContext())
        {
            var stored = Assert.Single(
                await context.RefreshTokens.IgnoreQueryFilters()
                    .Where(token => token.Token == tokenHash)
                    .ToListAsync());
            Assert.NotNull(stored.RevokedAt);
            // datetime2(0) rounds to the second on save, so the read-back
            // value may sit up to half a second ahead of the test clock.
            Assert.True(stored.RevokedAt <= DateTime.UtcNow.AddSeconds(1));
        }

        // Task 1.3 regression: a revoked token no longer authenticates. This
        // call clears the captured logs, so the log assertion below runs first.
        Assert.Contains(Logs, entry =>
            entry.Level == LogLevel.Information
            && entry.Category.EndsWith("LogoutCommandHandler", StringComparison.Ordinal)
            && entry.Message.Contains("Logout succeeded", StringComparison.Ordinal));

        var refresh = await PostRefreshAsync(refreshCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Logout_WithAlreadyRevokedToken_ShouldStillReturn204()
    {
        await ProvisionTenantAsync(adminEmail: "doctor@clinic.com");
        var (accessToken, firstCookie) = await LoginForSessionAsync("doctor@clinic.com", DefaultPassword);

        // Rotation (task 1.3) revokes the first token while issuing a new one.
        var refresh = await PostRefreshAsync(firstCookie);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

        var response = await LogoutAsync(accessToken, firstCookie);

        // Idempotent: an already-revoked token changes nothing and still
        // answers 204, never leaking session state (task 2.2 brief).
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutCookie_ShouldReturn204()
    {
        await ProvisionTenantAsync(adminEmail: "doctor@clinic.com");
        var (accessToken, _) = await LoginForSessionAsync("doctor@clinic.com", DefaultPassword);

        var response = await LogoutAsync(accessToken, cookieValue: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutJwt_ShouldReturn401()
    {
        var response = await Client.PostAsync("/api/v1/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
