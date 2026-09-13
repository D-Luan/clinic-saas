using HealthBr.Application.Common.Security;
using HealthBr.Infrastructure.MultiTenancy;

namespace HealthBr.Api.Middlewares;

/// <summary>
/// Sole writer of the tenant id (spec 15.7): reads the <c>tenant_id</c> claim
/// from the authenticated JWT — identity data always comes from the token,
/// never from the request payload (spec 15.1). Requests without a usable
/// claim keep <see cref="Guid.Empty"/>, which the Global Query Filter matches
/// against nothing (fail-safe).
/// </summary>
public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claimValue = context.User.FindFirst(AuthConstants.TenantIdClaim)?.Value;
            if (Guid.TryParse(claimValue, out var tenantId))
            {
                tenantContext.SetTenantId(tenantId);
            }
        }

        await _next(context);
    }
}
