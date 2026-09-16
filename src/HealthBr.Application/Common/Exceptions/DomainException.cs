namespace HealthBr.Application.Common.Exceptions;

/// <summary>
/// Base exception for business-rule violations. The global error pipeline
/// maps it to <c>409 Conflict</c> carrying the given <see cref="ErrorCode"/>
/// and user-facing message (spec 10.1), e.g.
/// <c>new DomainException("INVALID_STATUS_TRANSITION", "Não é possível mover de Completed para InProgress.")</c>.
/// </summary>
public class DomainException : Exception
{
    public string ErrorCode { get; }

    /// <summary>
    /// Optional structured context surfaced as the ProblemDetails
    /// <c>details</c> member (spec 10.1 example: from/to statuses).
    /// </summary>
    public IReadOnlyDictionary<string, string>? Details { get; }

    public DomainException(string errorCode, string message, IReadOnlyDictionary<string, string>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }
}
