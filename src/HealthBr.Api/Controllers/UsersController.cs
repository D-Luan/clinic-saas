using HealthBr.Application.Common.Security;
using HealthBr.Application.Features.Users.Commands;
using HealthBr.Application.Features.Users.Dto;
using HealthBr.Application.Features.Users.Queries;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthBr.Api.Controllers;

/// <summary>
/// Tenant user management (spec 5.2/9): listing and creation are Doctor-only.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = AuthConstants.DoctorOnlyPolicy)]
public sealed class UsersController(
    ListUsersQueryHandler listHandler,
    CreateUserCommandHandler createHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        // Spec 9 pagination: page default 1, pageSize default 20 max 100 —
        // normalized inside ListUsersQuery.
        var response = await listHandler.HandleAsync(new ListUsersQuery(page, pageSize), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        // Validation failures surface as ProblemDetails through the global
        // error pipeline (spec 10.1/15.2). The command carries no role and
        // no tenant: both are decided server-side (spec 15.1).
        var result = await createHandler.HandleAsync(
            new CreateUserCommand(request.Name, request.Email, request.Password),
            cancellationToken);

        // Spec 9: 201 with a Location header pointing at the user resource
        // URI (a per-user GET is not part of the MVP surface — same decision
        // as the tenants endpoint in task 2.1).
        return Created(
            $"/api/v1/users/{result.Id}",
            new UserResponse(result.Id, result.Name, result.Email, result.Role, result.CreatedAt));
    }
}
