namespace HealthBr.Application.Features.Tenants.Commands;

/// <summary>
/// Provisioning input. <see cref="AdminEmail"/> is normalized (trimmed,
/// lower-cased) to the same canonical form used by the login command, so the
/// global uniqueness check, persistence and login lookups all agree on a
/// single representation of the e-mail.
/// </summary>
public sealed record CreateTenantCommand
{
    public string ClinicName { get; }

    public string AdminName { get; }

    public string AdminEmail { get; }

    public string AdminPassword { get; }

    public CreateTenantCommand(string clinicName, string adminName, string adminEmail, string adminPassword)
    {
        ClinicName = clinicName;
        AdminName = adminName;
        AdminEmail = adminEmail.Trim().ToLowerInvariant();
        AdminPassword = adminPassword;
    }
}
