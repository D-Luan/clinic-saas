namespace HealthBr.Application.Features.Auth.Commands;

/// <summary>
/// Login input. The e-mail is normalized (trimmed, lower-cased) here so every
/// lookup uses a single canonical form; persistence must follow the same rule
/// (spec 15.2 — validation is re-applied regardless of client input).
/// </summary>
public sealed record LoginCommand
{
    public string Email { get; }

    public string Password { get; }

    public LoginCommand(string email, string password)
    {
        Email = email.Trim().ToLowerInvariant();
        Password = password;
    }
}
