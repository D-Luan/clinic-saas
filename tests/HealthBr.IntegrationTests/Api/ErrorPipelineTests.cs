using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using HealthBr.Api.Middlewares;
using HealthBr.IntegrationTests.Persistence;

namespace HealthBr.IntegrationTests.Api;

/// <summary>
/// Covers the global error pipeline, health checks and security headers
/// introduced by task 1.4 (spec 10.1, 3.2, 15.4).
/// </summary>
public sealed class ErrorPipelineTests(SqlServerFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task Login_WithInvalidPayload_ShouldReturn400ProblemDetailsWithPerFieldDetails()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "not-an-email", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("errorCode").GetString());
        Assert.Equal("https://errors.healthbr.dev/VALIDATION_ERROR", body.GetProperty("type").GetString());
        Assert.Equal("Dados inválidos.", body.GetProperty("message").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
        Assert.NotEmpty(body.GetProperty("details").GetProperty("Email").EnumerateArray());
        Assert.NotEmpty(body.GetProperty("details").GetProperty("Password").EnumerateArray());
    }

    [Fact]
    public async Task Login_WithMalformedJson_ShouldReturn400ProblemDetails()
    {
        using var content = new StringContent("{ not valid json", Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401ProblemDetails()
    {
        await SeedUserAsync(Guid.NewGuid(), "doctor@clinic.com");

        var response = await Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "doctor@clinic.com", password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("INVALID_CREDENTIALS", body.GetProperty("errorCode").GetString());
        Assert.Equal("Credenciais inválidas.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task HealthEndpoints_ShouldReturn200WithoutAuth()
    {
        var self = await Client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, self.StatusCode);

        // /health/ready pings the Testcontainers SQL Server bound to the
        // test host (spec 3.2).
        var ready = await Client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Fact]
    public async Task SecurityHeaders_ShouldBePresentOnSuccessAndErrorResponses()
    {
        var success = await Client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        AssertSecurityHeaders(success);

        var error = await Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "not-an-email", password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        AssertSecurityHeaders(error);
    }

    private static void AssertSecurityHeaders(HttpResponseMessage response)
    {
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal(SecurityHeadersMiddleware.StrictCsp, response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("geolocation=(), microphone=(), camera=()", response.Headers.GetValues("Permissions-Policy").Single());
    }
}
