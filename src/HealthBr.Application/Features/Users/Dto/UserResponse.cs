namespace HealthBr.Application.Features.Users.Dto;

/// <summary>
/// User shape exposed by <c>GET /api/v1/users</c> items and the
/// <c>POST /api/v1/users</c> response (spec 9). <c>PasswordHash</c> never
/// leaves the persistence layer.
/// </summary>
public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAt);
