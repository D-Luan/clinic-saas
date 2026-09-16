namespace HealthBr.Application.Features.Users.Dto;

/// <summary>
/// Result of <c>POST /api/v1/users</c>: the created user as persisted,
/// including the server-assigned Receptionist role, for the 201 body and
/// Location header.
/// </summary>
public sealed record CreateUserResult(
    Guid Id,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAt);
