namespace HealthBr.Application.Features.Tenants.Dto;

/// <summary>
/// Body of the public provisioning endpoint <c>POST /api/v1/tenants</c>
/// (spec 5.1): clinic data plus the credentials of the first admin user.
/// <see cref="AdminEmail"/> is normalized in the constructor (trimmed,
/// lower-cased) so validation runs on the canonical form that is persisted —
/// the same form the login command derives — and a padded or mixed-case
/// e-mail passes the format check instead of being rejected.
/// </summary>
public sealed record CreateTenantRequest
{
    public string ClinicName { get; }

    public string AdminName { get; }

    public string AdminEmail { get; }

    public string AdminPassword { get; }

    public CreateTenantRequest(string clinicName, string adminName, string adminEmail, string adminPassword)
    {
        ClinicName = clinicName;
        AdminName = adminName;
        AdminEmail = (adminEmail ?? string.Empty).Trim().ToLowerInvariant();
        AdminPassword = adminPassword;
    }
}
