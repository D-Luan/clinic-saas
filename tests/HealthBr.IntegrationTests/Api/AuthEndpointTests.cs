using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using HealthBr.IntegrationTests.Persistence;

using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HealthBr.IntegrationTests.Api;

public sealed class AuthEndpointTests(SqlServerFixture fixture) : ApiTestBase(fixture)
{
    private async Task<HttpResponseMessage> LoginAsync(string email, string password)
    {
        ClearLogs();
        return await Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnAccessTokenWithSpecClaims()
    {
        var tenantId = Guid.NewGuid();
        var user = await SeedUserAsync(tenantId, "doctor@clinic.com");

        var response = await LoginAsync("doctor@clinic.com", DefaultPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = new JsonWebTokenHandler().ReadJsonWebToken(await ReadAccessTokenAsync(response));
        Assert.Equal(user.Id.ToString(), token.GetClaim("sub")!.Value);
        Assert.Equal("doctor@clinic.com", token.GetClaim("email")!.Value);
        Assert.Equal("Doctor", token.GetClaim("role")!.Value);
        Assert.Equal(tenantId.ToString(), token.GetClaim("tenant_id")!.Value);
        Assert.InRange((token.ValidTo - token.ValidFrom).TotalMinutes, 14.9, 15.1);
        Assert.True(token.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldIssueTokenWithValidSignature()
    {
        var tenantId = Guid.NewGuid();
        await SeedUserAsync(tenantId, "doctor@clinic.com");

        var response = await LoginAsync("doctor@clinic.com", DefaultPassword);
        var accessToken = await ReadAccessTokenAsync(response);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(accessToken, new TokenValidationParameters
        {
            ValidIssuer = "healthbr",
            ValidateIssuer = true,
            ValidAudience = "healthbr-web",
            ValidateAudience = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthApiFactory.TestSigningKey)),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
        });

        Assert.True(result.IsValid, result.Exception?.Message);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldSetRefreshTokenCookieWithSpecAttributes()
    {
        var tenantId = Guid.NewGuid();
        await SeedUserAsync(tenantId, "doctor@clinic.com");

        var response = await LoginAsync("doctor@clinic.com", DefaultPassword);

        // Spec 15.5: HttpOnly, Secure, SameSite=Strict, Path=/api/v1/auth.
        var setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("refresh_token=", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(ExtractRefreshToken(response));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldLogSuccessEvent()
    {
        var tenantId = Guid.NewGuid();
        await SeedUserAsync(tenantId, "doctor@clinic.com");

        await LoginAsync("doctor@clinic.com", DefaultPassword);

        Assert.Contains(Logs, entry =>
            entry.Level == LogLevel.Information
            && entry.Category.EndsWith("LoginCommandHandler", StringComparison.Ordinal)
            && entry.Message.Contains("doctor@clinic.com")
            && entry.Message.Contains("succeeded", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401AndLogWarning()
    {
        var tenantId = Guid.NewGuid();
        await SeedUserAsync(tenantId, "doctor@clinic.com");

        var response = await LoginAsync("doctor@clinic.com", "WrongPassword1!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(Logs, entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("doctor@clinic.com"));
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShouldBeIndistinguishableFromWrongPassword()
    {
        var tenantId = Guid.NewGuid();
        await SeedUserAsync(tenantId, "doctor@clinic.com");

        var wrongPassword = await LoginAsync("doctor@clinic.com", "WrongPassword1!");
        var unknownEmail = await LoginAsync("ghost@clinic.com", "WrongPassword1!");

        Assert.Equal(wrongPassword.StatusCode, unknownEmail.StatusCode);
        // The ProblemDetails bodies match on every field except traceId,
        // which is unique per request by design (spec 10.1) and carries no
        // information about the account.
        Assert.Equal(
            await BodyWithoutTraceIdAsync(wrongPassword),
            await BodyWithoutTraceIdAsync(unknownEmail));
        Assert.Contains(Logs, entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("ghost@clinic.com"));
    }

    private static async Task<string> BodyWithoutTraceIdAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var mutable = JsonSerializer.SerializeToNode(payload);
        mutable!.AsObject().Remove("traceId");
        return mutable.ToJsonString();
    }

    [Fact]
    public async Task Login_WithDuplicatedEmailAcrossTenants_ShouldReturnGeneric401()
    {
        await SeedUserAsync(Guid.NewGuid(), "shared@clinic.com");
        await SeedUserAsync(Guid.NewGuid(), "shared@clinic.com");

        var response = await LoginAsync("shared@clinic.com", DefaultPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(Logs, entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("ambiguous", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_WithInvalidPayload_ShouldReturn400()
    {
        ClearLogs();
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new { email = "not-an-email", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
