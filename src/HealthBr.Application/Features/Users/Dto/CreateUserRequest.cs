namespace HealthBr.Application.Features.Users.Dto;

/// <summary>
/// Body of <c>POST /api/v1/users</c> (spec 9: creates a Receptionist).
/// <see cref="Email"/> is normalized in the constructor (trimmed,
/// lower-cased) to the canonical form persisted and matched by the login
/// lookup, mirroring the tenant provisioning request. There is deliberately
/// no <c>role</c> member: the role is a server-side decision (spec 15.1
/// anti-escalation) and any <c>role</c> key sent by the client is ignored by
/// JSON binding.
/// </summary>
public sealed record CreateUserRequest
{
    public string Name { get; }

    public string Email { get; }

    public string Password { get; }

    public CreateUserRequest(string name, string email, string password)
    {
        Name = name;
        Email = (email ?? string.Empty).Trim().ToLowerInvariant();
        Password = password;
    }
}
