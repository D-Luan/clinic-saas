namespace HealthBr.Infrastructure.Auth;

/// <summary>
/// JWT configuration bound from the <c>Jwt</c> section. Issuer and audience
/// are not secrets and live in <c>appsettings.json</c>;
/// <see cref="SigningKey"/> is a 256-bit secret that must come from
/// user-secrets (dev) or Key Vault (prod) — never committed (spec 12.2/15.8).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "healthbr";

    public string Audience { get; init; } = "healthbr-web";

    public string SigningKey { get; init; } = string.Empty;
}
