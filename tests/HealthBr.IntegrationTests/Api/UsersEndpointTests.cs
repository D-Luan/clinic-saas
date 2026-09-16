using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using HealthBr.Domain.Enums;
using HealthBr.Infrastructure.Auth;
using HealthBr.IntegrationTests.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthBr.IntegrationTests.Api;

/// <summary>
/// Covers <c>GET/POST /api/v1/users</c> (task 2.2, spec 5.2/9): Doctor-only
/// management of tenant users, Receptionist-only creation with global e-mail
/// uniqueness, the standard pagination contract, tenant isolation and the
/// 403 security-event log.
/// </summary>
public sealed class UsersEndpointTests(SqlServerFixture fixture) : ApiTestBase(fixture)
{
    private async Task<(Guid TenantId, string DoctorToken)> ProvisionDoctorAsync(string adminEmail)
    {
        var tenantId = await ProvisionTenantAsync(adminEmail: adminEmail);
        var accessToken = await LoginForAccessTokenAsync(adminEmail, DefaultPassword);
        return (tenantId, accessToken);
    }

    private async Task<HttpResponseMessage> CreateReceptionistAsync(
        string doctorToken,
        string email,
        string name = "Rita Reception",
        string password = "Reception1",
        object? extraPayload = null)
    {
        ClearLogs();
        var payload = new Dictionary<string, object>
        {
            ["name"] = name,
            ["email"] = email,
            ["password"] = password,
        };

        // Extra keys model malicious payloads (e.g. "role": "Doctor") the DTO
        // must ignore — JSON binding skips unknown members.
        if (extraPayload is not null)
        {
            foreach (var property in JsonSerializer.SerializeToElement(extraPayload).EnumerateObject())
            {
                payload[property.Name] = property.Value;
            }
        }

        using var request = AuthorizedRequest(HttpMethod.Post, "/api/v1/users", doctorToken, payload);
        return await Client.SendAsync(request);
    }

    [Fact]
    public async Task List_AsDoctor_ShouldReturnPaginatedUsersOfOwnTenant()
    {
        var (_, doctorToken) = await ProvisionDoctorAsync("doctor@clinic.com");
        Assert.True(HttpStatusCode.Created == (await CreateReceptionistAsync(doctorToken, "recep1@clinic.com")).StatusCode);
        Assert.True(HttpStatusCode.Created == (await CreateReceptionistAsync(doctorToken, "recep2@clinic.com")).StatusCode);

        using var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/users", doctorToken);
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Spec 9/7.5: the standard list contract.
        Assert.Equal(1, body.GetProperty("page").GetInt32());
        Assert.Equal(20, body.GetProperty("pageSize").GetInt32());
        Assert.Equal(3, body.GetProperty("total").GetInt32());
        Assert.Equal(1, body.GetProperty("totalPages").GetInt32());

        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(3, items.Count);
        Assert.Contains(items, item => item.GetProperty("role").GetString() == "Doctor");
        Assert.Equal(2, items.Count(item => item.GetProperty("role").GetString() == "Receptionist"));
        Assert.All(items, item =>
        {
            Assert.NotEqual(Guid.Empty, item.GetProperty("id").GetGuid());
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("name").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("email").GetString()));
            Assert.NotEqual(default, item.GetProperty("createdAt").GetDateTime());
        });

        // Persistence internals never leave the API: no password hash and no
        // tenant discriminator in the list items.
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenantId", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task List_WithPagingParameters_ShouldRespectPageAndClampPageSize()
    {
        var (_, doctorToken) = await ProvisionDoctorAsync("doctor@clinic.com");
        await CreateReceptionistAsync(doctorToken, "recep1@clinic.com");
        await CreateReceptionistAsync(doctorToken, "recep2@clinic.com");

        // Page 2 with pageSize 2: only the last user in creation order.
        using var paged = AuthorizedRequest(HttpMethod.Get, "/api/v1/users?page=2&pageSize=2", doctorToken);
        var pagedResponse = await Client.SendAsync(paged);
        Assert.Equal(HttpStatusCode.OK, pagedResponse.StatusCode);
        var body = await pagedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body.GetProperty("page").GetInt32());
        Assert.Equal(2, body.GetProperty("pageSize").GetInt32());
        Assert.Equal(3, body.GetProperty("total").GetInt32());
        Assert.Equal(2, body.GetProperty("totalPages").GetInt32());
        var item = Assert.Single(body.GetProperty("items").EnumerateArray());
        Assert.Equal("recep2@clinic.com", item.GetProperty("email").GetString());

        // Spec 9: pageSize clamped at 100.
        using var clamped = AuthorizedRequest(HttpMethod.Get, "/api/v1/users?pageSize=150", doctorToken);
        var clampedResponse = await Client.SendAsync(clamped);
        var clampedBody = await clampedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100, clampedBody.GetProperty("pageSize").GetInt32());

        // Out-of-range values fall back to the defaults (page 1, pageSize 20).
        using var defaulted = AuthorizedRequest(HttpMethod.Get, "/api/v1/users?page=0&pageSize=0", doctorToken);
        var defaultedResponse = await Client.SendAsync(defaulted);
        var defaultedBody = await defaultedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, defaultedBody.GetProperty("page").GetInt32());
        Assert.Equal(20, defaultedBody.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task List_AsReceptionist_ShouldReturn403AndLogDeniedAccess()
    {
        var (_, doctorToken) = await ProvisionDoctorAsync("doctor@clinic.com");
        await CreateReceptionistAsync(doctorToken, "reception@clinic.com");
        var receptionistToken = await LoginForAccessTokenAsync("reception@clinic.com", "Reception1");

        ClearLogs();
        using var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/users", receptionistToken);
        var response = await Client.SendAsync(request);

        // Spec 5.2: user management is Doctor-only; the backend is the source
        // of truth regardless of what the UI hides.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Spec 15.6: the denied access is a security event carrying the
        // endpoint and the caller's role.
        Assert.Contains(Logs, entry =>
            entry.Level == LogLevel.Warning
            && entry.Category == "HealthBr.Api.Auth"
            && entry.Message.Contains("Access denied", StringComparison.Ordinal)
            && entry.Message.Contains("/api/v1/users", StringComparison.Ordinal)
            && entry.Message.Contains("Receptionist", StringComparison.Ordinal));
    }

    [Fact]
    public async Task List_WithoutToken_ShouldReturn401()
    {
        var response = await Client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithTwoTenantsProvisioned_ShouldIsolateUsersPerTenant()
    {
        var (_, doctorTokenA) = await ProvisionDoctorAsync("doctor-a@clinic.com");
        var (_, doctorTokenB) = await ProvisionDoctorAsync("doctor-b@clinic.com");
        await CreateReceptionistAsync(doctorTokenA, "recep-a@clinic.com");
        await CreateReceptionistAsync(doctorTokenB, "recep-b@clinic.com");

        var listA = await ListEmailsAsync(doctorTokenA);
        var listB = await ListEmailsAsync(doctorTokenB);

        // Spec 15.7: each tenant sees only its own users (Global Query Filter
        // backed by the token's tenant_id claim).
        Assert.Equal(["doctor-a@clinic.com", "recep-a@clinic.com"], listA);
        Assert.Equal(["doctor-b@clinic.com", "recep-b@clinic.com"], listB);
    }

    private async Task<string[]> ListEmailsAsync(string accessToken)
    {
        using var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/users", accessToken);
        var response = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("email").GetString())
            .OrderBy(email => email, StringComparer.Ordinal)
            .ToArray()!;
    }

    [Fact]
    public async Task Create_AsDoctorWithRoleEscalationAttempt_ShouldCreateReceptionistAndAllowLogin()
    {
        var (tenantId, doctorToken) = await ProvisionDoctorAsync("doctor@clinic.com");
        var before = DateTime.UtcNow.AddSeconds(-5);

        // The payload tries to escalate; the DTO has no role member, so JSON
        // binding ignores it (spec 15.1).
        var response = await CreateReceptionistAsync(
            doctorToken,
            "newrecep@clinic.com",
            extraPayload: new { role = "Doctor", tenantId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var userId = body.GetProperty("id").GetGuid();
        Assert.Equal($"/api/v1/users/{userId}", response.Headers.Location?.ToString());
        Assert.Equal("Receptionist", body.GetProperty("role").GetString());
        Assert.Equal("newrecep@clinic.com", body.GetProperty("email").GetString());

        var after = DateTime.UtcNow.AddSeconds(5);
        await using var context = CreateContext(tenant => tenant.SetTenantId(tenantId));
        var user = Assert.Single(await context.Users.Where(u => u.Id == userId).ToListAsync());

        Assert.Equal(tenantId, user.TenantId); // tenant from the JWT, not the payload
        Assert.Equal(UserRole.Receptionist, user.Role); // role is a server-side decision
        Assert.False(user.IsDeleted);
        Assert.InRange(user.CreatedAt, before, after);
        Assert.InRange(user.UpdatedAt, before, after);

        // Hash persisted and verifiable — never the plain password.
        Assert.NotEqual("Reception1", user.PasswordHash);
        Assert.True(new PasswordHasher().VerifyHashedPassword(user.PasswordHash, "Reception1"));

        // Spec 2/5.1: no e-mail confirmation — the new user can log in at once.
        var login = await Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "newrecep@clinic.com", password = "Reception1" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // Spec 15.6: user creation is a security event.
        Assert.Contains(Logs, entry =>
            entry.Level == LogLevel.Information
            && entry.Category.EndsWith("CreateUserCommandHandler", StringComparison.Ordinal)
            && entry.Message.Contains(userId.ToString()));
    }

    [Fact]
    public async Task Create_AsReceptionist_ShouldReturn403()
    {
        var (_, doctorToken) = await ProvisionDoctorAsync("doctor@clinic.com");
        await CreateReceptionistAsync(doctorToken, "reception@clinic.com");
        var receptionistToken = await LoginForAccessTokenAsync("reception@clinic.com", "Reception1");

        using var request = AuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/users",
            receptionistToken,
            new { name = "Another One", email = "another@clinic.com", password = "Another1" });
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutToken_ShouldReturn401()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/v1/users",
            new { name = "Anyone", email = "anyone@clinic.com", password = "Anyone12" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDuplicatedEmailInSameTenant_ShouldReturn409()
    {
        var (_, doctorToken) = await ProvisionDoctorAsync("doctor@clinic.com");
        var first = await CreateReceptionistAsync(doctorToken, "taken@clinic.com");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await CreateReceptionistAsync(doctorToken, "taken@clinic.com");

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);
        var body = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("USER_EMAIL_ALREADY_EXISTS", body.GetProperty("errorCode").GetString());
    }

    // The uniqueness check is global (task 2.2 decision): the login lookup is
    // global and an e-mail active in ANOTHER tenant would create an account
    // that could never log in (ambiguous login -> 401).
    [Fact]
    public async Task Create_WithDuplicatedEmailInOtherTenant_ShouldReturn409WithGenericMessage()
    {
        await ProvisionDoctorAsync("doctor-a@clinic.com");
        var (tenantIdB, doctorTokenB) = await ProvisionDoctorAsync("doctor-b@clinic.com");

        var response = await CreateReceptionistAsync(doctorTokenB, "doctor-a@clinic.com");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("USER_EMAIL_ALREADY_EXISTS", body.GetProperty("errorCode").GetString());

        // The message is generic on purpose: it must not reveal in which
        // tenant the e-mail already exists (spec 15.1).
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(tenantIdB.ToString(), raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_WithInvalidPayload_ShouldReturn400WithPerFieldDetails()
    {
        var (_, doctorToken) = await ProvisionDoctorAsync("doctor@clinic.com");

        var response = await CreateReceptionistAsync(
            doctorToken,
            email: "not-an-email",
            name: "",
            password: "weak");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("errorCode").GetString());
        var details = body.GetProperty("details");
        Assert.NotEmpty(details.GetProperty("Name").EnumerateArray());
        Assert.NotEmpty(details.GetProperty("Email").EnumerateArray());
        Assert.NotEmpty(details.GetProperty("Password").EnumerateArray());
    }
}
