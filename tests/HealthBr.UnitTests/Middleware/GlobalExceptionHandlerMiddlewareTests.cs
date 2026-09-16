using System.Security.Claims;
using System.Text.Json;

using FluentValidation;
using FluentValidation.Results;

using HealthBr.Api.Middlewares;
using HealthBr.Application.Common.Exceptions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace HealthBr.UnitTests.Middleware;

/// <summary>
/// Unit coverage for the exception -> ProblemDetails mapping of the global
/// error pipeline (spec 10.1), asserting the exact wire format: type, title,
/// status, errorCode, message, details and traceId.
/// </summary>
public sealed class GlobalExceptionHandlerMiddlewareTests
{
    private sealed record PipelineResult(int Status, string ContentType, JsonElement Body, string RawBody);

    private static async Task<PipelineResult> InvokeAsync(Exception exception, bool authenticated = false)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/v1/test";
        context.Response.Body = new MemoryStream();
        if (authenticated)
        {
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim("sub", "00000000-0000-0000-0000-000000000001")], "Testing"));
        }

        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw exception,
            NullLogger<GlobalExceptionHandlerMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var raw = await reader.ReadToEndAsync();
        return new PipelineResult(
            context.Response.StatusCode,
            context.Response.ContentType!,
            JsonSerializer.Deserialize<JsonElement>(raw),
            raw);
    }

    [Fact]
    public async Task ValidationException_ShouldMapTo400WithPerFieldDetails()
    {
        var exception = new ValidationException(
        [
            new ValidationFailure("Email", "'Email' é um endereço de email inválido."),
            new ValidationFailure("Email", "erro acumulado do mesmo campo"),
            new ValidationFailure("Password", "'Password' não pode ser vazio."),
        ]);

        var result = await InvokeAsync(exception);

        Assert.Equal(400, result.Status);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal("VALIDATION_ERROR", result.Body.GetProperty("errorCode").GetString());
        Assert.Equal("https://errors.healthbr.dev/VALIDATION_ERROR", result.Body.GetProperty("type").GetString());
        Assert.Equal("Requisição inválida", result.Body.GetProperty("title").GetString());
        Assert.Equal("Dados inválidos.", result.Body.GetProperty("message").GetString());
        Assert.False(string.IsNullOrWhiteSpace(result.Body.GetProperty("traceId").GetString()));

        var details = result.Body.GetProperty("details");
        var emailErrors = details.GetProperty("Email").EnumerateArray().Select(error => error.GetString()).ToArray();
        Assert.Equal(2, emailErrors!.Length);
        Assert.Contains("'Email' é um endereço de email inválido.", emailErrors);
        Assert.Contains("erro acumulado do mesmo campo", emailErrors);
        Assert.Single(details.GetProperty("Password").EnumerateArray());
    }

    [Fact]
    public async Task InvalidCredentialsException_ShouldMapToGeneric401()
    {
        var result = await InvokeAsync(new InvalidCredentialsException());

        Assert.Equal(401, result.Status);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal("INVALID_CREDENTIALS", result.Body.GetProperty("errorCode").GetString());
        Assert.Equal("Credenciais inválidas.", result.Body.GetProperty("message").GetString());
        Assert.False(string.IsNullOrWhiteSpace(result.Body.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task InvalidCredentialsException_WithCustomMessage_ShouldCarryItThrough()
    {
        var result = await InvokeAsync(new InvalidCredentialsException("Sessão expirada."));

        Assert.Equal(401, result.Status);
        Assert.Equal("Sessão expirada.", result.Body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task EntityNotFoundException_ShouldMapTo404()
    {
        var result = await InvokeAsync(new EntityNotFoundException("Paciente", Guid.NewGuid()));

        Assert.Equal(404, result.Status);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal("ENTITY_NOT_FOUND", result.Body.GetProperty("errorCode").GetString());
        Assert.Equal("https://errors.healthbr.dev/ENTITY_NOT_FOUND", result.Body.GetProperty("type").GetString());
        Assert.Contains("Paciente", result.Body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task DomainException_ShouldMapTo409WithItsErrorCodeAndDetails()
    {
        var exception = new DomainException(
            "INVALID_STATUS_TRANSITION",
            "Não é possível mover de Completed para InProgress.",
            new Dictionary<string, string> { ["from"] = "Completed", ["to"] = "InProgress" });

        var result = await InvokeAsync(exception);

        Assert.Equal(409, result.Status);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal("INVALID_STATUS_TRANSITION", result.Body.GetProperty("errorCode").GetString());
        Assert.Equal("https://errors.healthbr.dev/INVALID_STATUS_TRANSITION", result.Body.GetProperty("type").GetString());
        Assert.Equal("Não é possível mover de Completed para InProgress.", result.Body.GetProperty("message").GetString());
        Assert.Equal("Completed", result.Body.GetProperty("details").GetProperty("from").GetString());
        Assert.Equal("InProgress", result.Body.GetProperty("details").GetProperty("to").GetString());
    }

    [Fact]
    public async Task UnauthorizedAccessException_WhenAnonymous_ShouldMapTo401()
    {
        var result = await InvokeAsync(new UnauthorizedAccessException());

        Assert.Equal(401, result.Status);
        Assert.Equal("UNAUTHORIZED", result.Body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UnauthorizedAccessException_WhenAuthenticated_ShouldMapTo403()
    {
        var result = await InvokeAsync(new UnauthorizedAccessException(), authenticated: true);

        Assert.Equal(403, result.Status);
        Assert.Equal("FORBIDDEN", result.Body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UnhandledException_ShouldMapToGeneric500WithoutLeakingDetails()
    {
        var result = await InvokeAsync(new InvalidOperationException("boom with internal state"));

        Assert.Equal(500, result.Status);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal("INTERNAL_ERROR", result.Body.GetProperty("errorCode").GetString());
        Assert.Equal("https://errors.healthbr.dev/INTERNAL_ERROR", result.Body.GetProperty("type").GetString());
        Assert.Equal("Erro interno. Tente novamente mais tarde.", result.Body.GetProperty("message").GetString());
        Assert.DoesNotContain("boom", result.RawBody, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.Body.TryGetProperty("details", out _));
        Assert.False(string.IsNullOrWhiteSpace(result.Body.GetProperty("traceId").GetString()));
    }
}
