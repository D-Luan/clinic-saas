namespace HealthBr.Application.Common.Security;

/// <summary>
/// Auth values fixed by the spec (sections 5.1 and 15.5). Lifetimes and the
/// cookie contract are single-sourced here so the token issuer, the use cases
/// and the cookie writer can never drift apart.
/// </summary>
public static class AuthConstants
{
    public const int AccessTokenMinutes = 15;

    public const int RefreshTokenDays = 7;

    public const string RefreshTokenCookieName = "refresh_token";

    public const string RefreshTokenCookiePath = "/api/v1/auth";

    // JWT claim names (spec 5.1). The Api disables inbound claim-type mapping
    // so these names arrive on User claims exactly as issued.
    public const string TenantIdClaim = "tenant_id";

    public const string RoleClaim = "role";

    /// <summary>
    /// Truncated token representation safe for logs: at most the first 8
    /// characters followed by "..." (spec 15.6 — never log full tokens).
    /// </summary>
    public static string TokenPreview(string token) => token.Length <= 8 ? "..." : $"{token[..8]}...";
}
