using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using HealthBr.Domain.Enums;
using HealthBr.Infrastructure.Auth;
using HealthBr.Infrastructure.MultiTenancy;
using HealthBr.IntegrationTests.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthBr.IntegrationTests.Api;

/// <summary>
/// Covers the public provisioning endpoint <c>POST /api/v1/tenants</c>
/// (task 2.1, spec 5.1/9/15.6): tenant + first Doctor user in a single
/// save, global admin e-mail uniqueness, immediate login after signup,
/// canonical e-mail normalization and the tenant-creation log event.
/// </summary>
public sealed class TenantProvisioningEndpointTests(SqlServerFixture fixture) : ApiTestBase(fixture)
{
    private const string ValidPassword = "Doctor@123";

    private async Task<HttpResponseMessage> ProvisionAsync(
        string clinicName = "Clínica Exemplo",
        string adminName = "Dra. Ana Souza",
        string adminEmail = "admin@exemplo.com",
        string adminPassword = ValidPassword)
    {
        ClearLogs();
        return await Client.PostAsJsonAsync("/api/v1/tenants", new { clinicName, adminName, adminEmail, adminPassword });
    }

    private static async Task<Guid> ReadTenantIdAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("tenantId").GetGuid();
    }

    [Fact]
    public async Task Provision_WithValidPayload_ShouldReturn201WithLocationAndPersistTenantAndAdminUser()
    {
        var before = DateTime.UtcNow.AddSeconds(-5);

        var response = await ProvisionAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var tenantId = await ReadTenantIdAsync(response);
        Assert.NotEqual(Guid.Empty, tenantId);
        Assert.Equal($"/api/v1/tenants/{tenantId}", response.Headers.Location?.ToString());

        await using var context = CreateContext(tenant => tenant.SetTenantId(tenantId));
        var tenant = Assert.Single(await context.Tenants.ToListAsync());
        var user = Assert.Single(await context.Users.ToListAsync());
        var after = DateTime.UtcNow.AddSeconds(5);

        Assert.Equal("Clínica Exemplo", tenant.Name);
        Assert.Equal(tenantId, tenant.Id);
        Assert.Equal(tenant.Id, tenant.TenantId); // PR #3: the tenant is its own tenant
        Assert.False(tenant.IsDeleted);
        Assert.InRange(tenant.CreatedAt, before, after);
        Assert.InRange(tenant.UpdatedAt, before, after);

        Assert.Equal(tenantId, user.TenantId);
        Assert.Equal("Dra. Ana Souza", user.Name);
        Assert.Equal("admin@exemplo.com", user.Email);
        Assert.Equal(UserRole.Doctor, user.Role);
        Assert.False(user.IsDeleted);
        Assert.InRange(user.CreatedAt, before, after);
        Assert.InRange(user.UpdatedAt, before, after);
        Assert.Equal(tenant.CreatedAt, user.CreatedAt); // single save, one timestamp

        Assert.NotEqual(ValidPassword, user.PasswordHash);
        Assert.True(new PasswordHasher().VerifyHashedPassword(user.PasswordHash, ValidPassword));
    }

    [Fact]
    public async Task Provision_WithValidPayload_ShouldLogTenantCreationEvent()
    {
        var response = await ProvisionAsync(adminEmail: "logged@exemplo.com");
        var tenantId = await ReadTenantIdAsync(response);

        // Spec 15.6: tenant creation logged as Information with the tenant id;
        // trace id and origin IP ride the request log scope set in Program.cs.
        Assert.Contains(Logs, entry =>
            entry.Level == LogLevel.Information
            && entry.Category.EndsWith("CreateTenantCommandHandler", StringComparison.Ordinal)
            && entry.Message.Contains(tenantId.ToString())
            && entry.Message.Contains("logged@exemplo.com"));
    }

    [Fact]
    public async Task Provision_WithoutAuthorizationHeader_ShouldBeAcceptedAsPublicEndpoint()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/tenants")
        {
            Content = JsonContent.Create(new
            {
                clinicName = "Clínica Exemplo",
                adminName = "Dra. Ana Souza",
                adminEmail = "public@exemplo.com",
                adminPassword = ValidPassword,
            }),
        };
        Assert.False(request.Headers.Contains("Authorization"));

        var response = await Client.SendAsync(request);

        // Public endpoint (spec 9): no credentials exist yet at signup time.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // Spec 2: no e-mail confirmation — right after provisioning, the admin
    // must be able to authenticate with the credentials just created.
    [Fact]
    public async Task Provision_ThenLoginWithCreatedCredentials_ShouldSucceed()
    {
        var provisioned = await ProvisionAsync();
        Assert.Equal(HttpStatusCode.Created, provisioned.StatusCode);

        var login = await Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "admin@exemplo.com", password = ValidPassword });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.NotEmpty(await ReadAccessTokenAsync(login));
    }

    [Fact]
    public async Task Provision_WithDuplicatedAdminEmail_ShouldReturn409()
    {
        var first = await ProvisionAsync(adminEmail: "admin@exemplo.com");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await ProvisionAsync(
            clinicName: "Outra Clínica",
            adminName: "Dr. Bruno Lima",
            adminEmail: "admin@exemplo.com");

        // Uniqueness is global (spec 5.1): a second signup with the same
        // e-mail is rejected even though it targets a brand-new tenant.
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);
        var body = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ADMIN_EMAIL_ALREADY_EXISTS", body.GetProperty("errorCode").GetString());
        Assert.Contains(Logs, entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("admin@exemplo.com"));
    }

    [Fact]
    public async Task Provision_WithAdminEmailAlreadyUsedInAnotherTenant_ShouldReturn409()
    {
        await SeedUserAsync(Guid.NewGuid(), "taken@clinic.com");

        var response = await ProvisionAsync(adminEmail: "taken@clinic.com");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ADMIN_EMAIL_ALREADY_EXISTS", body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Provision_WithSoftDeletedUserEmail_ShouldBeAllowed()
    {
        var tenantId = Guid.NewGuid();
        var deleted = await SeedUserAsync(tenantId, "recycled@clinic.com");

        await using (var context = CreateContext())
        {
            var user = await context.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == deleted.Id);
            user.IsDeleted = true;
            await context.SaveChangesAsync();
        }

        // Only ACTIVE users block an e-mail (mirrors the filtered unique
        // index and the login lookup).
        var response = await ProvisionAsync(adminEmail: "recycled@clinic.com");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Provision_WithInvalidPayload_ShouldReturn400WithPerFieldDetails()
    {
        var response = await ProvisionAsync(
            clinicName: "",
            adminName: "",
            adminEmail: "not-an-email",
            adminPassword: "weak");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("errorCode").GetString());
        var details = body.GetProperty("details");
        Assert.NotEmpty(details.GetProperty("ClinicName").EnumerateArray());
        Assert.NotEmpty(details.GetProperty("AdminName").EnumerateArray());
        Assert.NotEmpty(details.GetProperty("AdminEmail").EnumerateArray());
        Assert.NotEmpty(details.GetProperty("AdminPassword").EnumerateArray());
    }

    [Fact]
    public async Task Provision_WithPaddedMixedCaseEmail_ShouldNormalizeAndAllowLogin()
    {
        var response = await ProvisionAsync(adminEmail: "  Admin@Exemplo.COM  ");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var tenantId = await ReadTenantIdAsync(response);

        // Persisted in the canonical form shared with the login lookup.
        await using var context = CreateContext(tenant => tenant.SetTenantId(tenantId));
        var user = Assert.Single(await context.Users.ToListAsync());
        Assert.Equal("admin@exemplo.com", user.Email);

        var login = await Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "admin@exemplo.com", password = ValidPassword });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }
}
