using HealthBr.Application.Common.Exceptions;
using HealthBr.Application.Common.Security;
using HealthBr.Application.Features.Users.Queries;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthBr.Api.Controllers;

/// <summary>
/// <c>GET /api/v1/me</c> (spec 9): data of the authenticated user, any role.
/// </summary>
[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class MeController(GetCurrentUserQueryHandler getCurrentUserHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        // Identity comes from the token only (spec 15.1): the sub claim is
        // read here and the query handler additionally scopes the lookup to
        // the tenant of the token itself.
        var claimValue = User.FindFirst(AuthConstants.UserIdClaim)?.Value;
        if (!Guid.TryParse(claimValue, out var userId))
        {
            // Defensive only: tokens validated by this API always carry sub.
            throw new InvalidCredentialsException("Autenticação necessária.");
        }

        var response = await getCurrentUserHandler.HandleAsync(new GetCurrentUserQuery(userId), cancellationToken);
        return Ok(response);
    }
}
