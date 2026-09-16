namespace HealthBr.Application.Features.Users.Commands;

/// <summary>
/// Input of <c>POST /api/v1/users</c>. <see cref="Email"/> is normalized
/// (trimmed, lower-cased) to the same canonical form used by the login
/// command and tenant provisioning, so the global uniqueness check,
/// persistence and login lookups all agree. The tenant and the role are not
/// inputs: they come from the authenticated context and the server-side
/// policy respectively (spec 15.1).
/// </summary>
public sealed record CreateUserCommand
{
    public string Name { get; }

    public string Email { get; }

    public string Password { get; }

    public CreateUserCommand(string name, string email, string password)
    {
        Name = name;
        Email = email.Trim().ToLowerInvariant();
        Password = password;
    }
}
