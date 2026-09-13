namespace HealthBr.Application.Features.Auth.Dto;

/// <summary>
/// Body of <c>POST /api/v1/auth/login</c>. The e-mail is normalized
/// (trimmed, lower-cased) before lookup; password hashing is case sensitive.
/// </summary>
public sealed record LoginRequest(string Email, string Password);
