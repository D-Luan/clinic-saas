namespace HealthBr.Application.Common.Exceptions;

/// <summary>
/// Thrown by auth use cases when credentials or refresh tokens fail
/// validation. The global error pipeline maps it to a <c>401</c> whose body
/// carries this exception's message — so it must stay generic for every
/// failure reason, never revealing whether the e-mail exists (spec 15.1).
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : this("Credenciais inválidas.")
    {
    }

    public InvalidCredentialsException(string message)
        : base(message)
    {
    }
}
