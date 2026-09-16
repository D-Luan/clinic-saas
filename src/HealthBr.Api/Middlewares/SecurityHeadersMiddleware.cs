namespace HealthBr.Api.Middlewares;

/// <summary>
/// Applies the mandatory security response headers (spec 15.4) to every
/// response. The CSP is the spec's literal policy with one documented
/// relaxation: outside Production, responses under <c>/swagger</c> allow
/// <c>script-src 'unsafe-inline'</c> because Swashbuckle's index.html boots
/// through an inline script — no header is ever dropped.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    public const string StrictCsp =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; frame-ancestors 'none';";

    public const string SwaggerCsp =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; frame-ancestors 'none';";

    private readonly RequestDelegate _next;
    private readonly bool _relaxCspForSwagger;

    public SecurityHeadersMiddleware(RequestDelegate next, bool relaxCspForSwagger)
    {
        _next = next;
        _relaxCspForSwagger = relaxCspForSwagger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Content-Security-Policy"] =
            _relaxCspForSwagger && context.Request.Path.StartsWithSegments("/swagger") ? SwaggerCsp : StrictCsp;
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        await _next(context);
    }
}
