using System.Diagnostics;
using System.Text.Json;

using FluentValidation;

using HealthBr.Api.ProblemDetails;
using HealthBr.Application.Common.Exceptions;

namespace HealthBr.Api.Middlewares;

/// <summary>
/// Global error pipeline (spec 10.1): maps every unhandled exception to an
/// RFC 7807 <c>application/problem+json</c> response with <c>errorCode</c>,
/// <c>message</c>, optional <c>details</c> and <c>traceId</c>. Stack traces
/// never reach the client — the 500 path is the only one that carries them,
/// straight to the log with the trace id (Application Insights lands with
/// task 7.x).
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware
{
    private const string ProblemContentType = "application/problem+json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            // The response is already on the wire; nothing left to rewrite, so
            // the host fails the request instead of returning a half-written
            // body.
            if (context.Response.HasStarted)
            {
                throw;
            }

            var problem = Map(context, exception);

            if (problem.Status >= 500)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception ({ErrorCode}) on {Method} {Path} [TraceId {TraceId}]",
                    problem.ErrorCode,
                    context.Request.Method,
                    context.Request.Path,
                    problem.TraceId);
            }

            context.Response.StatusCode = problem.Status;
            context.Response.ContentType = ProblemContentType;
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
        }
    }

    private static ErrorProblemDetails Map(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        return exception switch
        {
            ValidationException validation => new ErrorProblemDetails(
                "VALIDATION_ERROR",
                "Requisição inválida",
                StatusCodes.Status400BadRequest,
                "Dados inválidos.",
                validation.Errors
                    .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(failure => failure.ErrorMessage).ToArray()),
                traceId),

            // Every failure reason yields the same generic body (spec 15.1);
            // the distinguishing detail lives only in the Warning logs.
            InvalidCredentialsException => new ErrorProblemDetails(
                "INVALID_CREDENTIALS",
                "Não autenticado",
                StatusCodes.Status401Unauthorized,
                exception.Message,
                details: null,
                traceId),

            EntityNotFoundException => new ErrorProblemDetails(
                "ENTITY_NOT_FOUND",
                "Recurso não encontrado",
                StatusCodes.Status404NotFound,
                exception.Message,
                details: null,
                traceId),

            DomainException domain => new ErrorProblemDetails(
                domain.ErrorCode,
                "Conflito de domínio",
                StatusCodes.Status409Conflict,
                domain.Message,
                domain.Details,
                traceId),

            // 401 for anonymous callers, 403 for authenticated ones without
            // permission (spec 10.1 "conforme contexto").
            UnauthorizedAccessException => context.User.Identity?.IsAuthenticated == true
                ? new ErrorProblemDetails(
                    "FORBIDDEN",
                    "Acesso negado",
                    StatusCodes.Status403Forbidden,
                    "Você não tem permissão para executar esta ação.",
                    details: null,
                    traceId)
                : new ErrorProblemDetails(
                    "UNAUTHORIZED",
                    "Não autenticado",
                    StatusCodes.Status401Unauthorized,
                    "Autenticação necessária.",
                    details: null,
                    traceId),

            // Fail-safe generic body: no exception text, no stack trace
            // (spec 15.1).
            _ => new ErrorProblemDetails(
                "INTERNAL_ERROR",
                "Erro interno",
                StatusCodes.Status500InternalServerError,
                "Erro interno. Tente novamente mais tarde.",
                details: null,
                traceId),
        };
    }
}
