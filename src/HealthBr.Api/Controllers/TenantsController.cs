using HealthBr.Application.Features.Tenants.Commands;
using HealthBr.Application.Features.Tenants.Dto;

using Microsoft.AspNetCore.Mvc;

namespace HealthBr.Api.Controllers;

[ApiController]
[Route("api/v1/tenants")]
public sealed class TenantsController(CreateTenantCommandHandler createHandler) : ControllerBase
{
    // Public by design (spec 5.1/9): provisioning happens before any account
    // exists, so the action carries no [Authorize] — and no rate limiting,
    // which is out of the MVP scope (spec 2).
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        // Validation failures surface as ProblemDetails through the global
        // error pipeline (spec 10.1/15.2), same as every other endpoint.
        var result = await createHandler.HandleAsync(
            new CreateTenantCommand(request.ClinicName, request.AdminName, request.AdminEmail, request.AdminPassword),
            cancellationToken);

        // Spec 9: 201 with a Location header pointing at the tenant resource
        // URI (a GET for tenants is not part of the MVP surface).
        return Created($"/api/v1/tenants/{result.TenantId}", new CreateTenantResponse(result.TenantId));
    }
}
