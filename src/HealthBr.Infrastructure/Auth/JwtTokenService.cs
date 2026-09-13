using System.Security.Claims;
using System.Text;

using HealthBr.Application.Common.Interfaces;
using HealthBr.Application.Common.Security;
using HealthBr.Domain.Entities;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HealthBr.Infrastructure.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IAccessTokenService
{
    // HmacSha256 over the 256-bit signing key (spec 15.8).
    public string CreateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Value.Issuer,
            Audience = options.Value.Audience,
            NotBefore = now,
            Expires = now.AddMinutes(AuthConstants.AccessTokenMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.SigningKey)),
                SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(AuthConstants.RoleClaim, user.Role.ToString()),
                new Claim(AuthConstants.TenantIdClaim, user.TenantId.ToString()),
            ]),
        };

        // JsonWebTokenHandler writes claims verbatim — no claim-type mapping.
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
