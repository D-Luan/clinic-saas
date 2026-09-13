namespace HealthBr.Application.Common.Exceptions;

/// <summary>
/// Thrown by auth use cases when credentials or refresh tokens fail
/// validation. Callers must map it to the same generic 401 for every failure
/// reason, never revealing whether the e-mail exists (spec 15.1).
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Invalid credentials.")
    {
    }
}
