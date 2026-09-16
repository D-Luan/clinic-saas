using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using HealthBr.Application.Common.Security;
using HealthBr.Domain.Entities;
using HealthBr.Domain.Enums;
using HealthBr.Infrastructure.Auth;
using HealthBr.IntegrationTests.Persistence;

namespace HealthBr.IntegrationTests.Api;

[Collection(DatabaseCollection.Name)]
public abstract class ApiTestBase : DatabaseTestBase
{
    protected const string DefaultPassword = "Doctor@123";

    private readonly TestLoggerProvider _loggerProvider = new();

    private AuthApiFactory _factory = null!;

    private HttpClient _client = null!;

    protected ApiTestBase(SqlServerFixture fixture)
        : base(fixture)
    {
    }

    protected HttpClient Client => _client;

    /// <summary>
    /// Boots an extra Api host on top of the same connection/transaction
    /// under a different environment (Swagger visibility, CSP relaxation).
    /// Callers own the factory's lifetime.
    /// </summary>
    internal AuthApiFactory CreateFactory(string environment) =>
        new(Connection, Transaction, _loggerProvider, TenantProbe, environment);

    protected TenantProbe TenantProbe { get; } = new();

    protected IReadOnlyList<LogEntry> Logs => _loggerProvider.Entries;

    protected void ClearLogs() => _loggerProvider.Clear();

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Feeds the Program.cs startup guards (connection string + JWT
        // signing key) through a configuration source the deferred test host
        // reads before the application builder runs.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=placeholder-see-db-wiring");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", AuthApiFactory.TestSigningKey);

        _factory = new AuthApiFactory(Connection, Transaction, _loggerProvider, TenantProbe);
        _client = _factory.CreateClient();
    }

    public override async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await base.DisposeAsync();
    }

    protected async Task<User> SeedUserAsync(
        Guid tenantId,
        string email,
        string password = DefaultPassword,
        UserRole role = UserRole.Doctor)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Dra. Ana Souza",
            Email = email,
            PasswordHash = new PasswordHasher().HashPassword(password),
            Role = role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await using (var seed = CreateContext())
        {
            seed.Users.Add(user);
            await seed.SaveChangesAsync();
        }

        return user;
    }

    protected async Task<RefreshToken> SeedRefreshTokenAsync(
        User user,
        string tokenHash,
        DateTime? expiresAt = null,
        DateTime? revokedAt = null)
    {
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TenantId = user.TenantId,
            UserId = user.Id,
            Token = tokenHash,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(AuthConstants.RefreshTokenDays),
            RevokedAt = revokedAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await using (var seed = CreateContext())
        {
            seed.RefreshTokens.Add(entity);
            await seed.SaveChangesAsync();
        }

        return entity;
    }

    /// <summary>
    /// Reads the raw refresh token from the single Set-Cookie header. The
    /// cookie is Secure, so it is replayed manually via the Cookie header
    /// instead of relying on an HttpClient cookie container.
    /// </summary>
    protected static string ExtractRefreshToken(HttpResponseMessage response)
    {
        var setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("refresh_token=", setCookie, StringComparison.OrdinalIgnoreCase);
        var value = setCookie["refresh_token=".Length..];
        var end = value.IndexOf(';');
        return end < 0 ? value : value[..end];
    }

    protected static async Task<string> ReadAccessTokenAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("accessToken missing from response.");
    }

    protected async Task<HttpResponseMessage> PostRefreshAsync(string? cookieValue)
    {
        ClearLogs();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        if (cookieValue is not null)
        {
            request.Headers.TryAddWithoutValidation(
                "Cookie",
                $"{AuthConstants.RefreshTokenCookieName}={cookieValue}");
        }

        return await Client.SendAsync(request);
    }

    /// <summary>
    /// Provisions a tenant through the public endpoint (task 2.1 pattern) and
    /// returns its id — the authenticated surface of task 2.2 builds on it.
    /// </summary>
    protected async Task<Guid> ProvisionTenantAsync(
        string adminEmail,
        string clinicName = "Clínica Exemplo",
        string adminName = "Dra. Ana Souza")
    {
        var response = await Client.PostAsJsonAsync(
            "/api/v1/tenants",
            new { clinicName, adminName, adminEmail, adminPassword = DefaultPassword });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("tenantId").GetGuid();
    }

    protected async Task<string> LoginForAccessTokenAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAccessTokenAsync(response);
    }

    /// <summary>
    /// Full login returning the access token and the raw refresh token read
    /// from the Set-Cookie header (the cookie is Secure, so tests replay it
    /// manually).
    /// </summary>
    protected async Task<(string AccessToken, string RefreshCookie)> LoginForSessionAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await ReadAccessTokenAsync(response), ExtractRefreshToken(response));
    }

    protected static HttpRequestMessage AuthorizedRequest(HttpMethod method, string url, string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }
}
