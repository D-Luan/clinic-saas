namespace HealthBr.Application.Common.Exceptions;

/// <summary>
/// Thrown when a use case cannot find the requested entity. The global error
/// pipeline maps it to <c>404 Not Found</c> (spec 10.1). The message is
/// user-facing; cross-tenant lookups must fail indistinguishably from
/// unknown ids so resource existence is never leaked (spec 15.7).
/// </summary>
public sealed class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string message)
        : base(message)
    {
    }

    public EntityNotFoundException(string entityName, object key)
        : this($"'{entityName}' não foi encontrado para o identificador '{key}'.")
    {
    }
}
