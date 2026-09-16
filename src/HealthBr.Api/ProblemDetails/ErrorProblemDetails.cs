using System.Text.Json.Serialization;

namespace HealthBr.Api.ProblemDetails;

/// <summary>
/// RFC 7807 error body extended with the HealthBr fields (spec 10.1):
/// <c>errorCode</c>, user-facing <c>message</c>, optional structured
/// <c>details</c> and the <c>traceId</c> shared with server logs, so a
/// client-side report can be correlated with the logged exception.
/// Property order follows the spec example.
/// </summary>
public sealed class ErrorProblemDetails
{
    public string Type { get; init; }

    public string Title { get; init; }

    public int Status { get; init; }

    public string ErrorCode { get; init; }

    public string Message { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Details { get; init; }

    public string TraceId { get; init; }

    public ErrorProblemDetails(string errorCode, string title, int status, string message, object? details, string traceId)
    {
        Type = $"https://errors.healthbr.dev/{errorCode}";
        Title = title;
        Status = status;
        ErrorCode = errorCode;
        Message = message;
        Details = details;
        TraceId = traceId;
    }
}
