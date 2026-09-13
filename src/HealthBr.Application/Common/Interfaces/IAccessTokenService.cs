using HealthBr.Domain.Entities;

namespace HealthBr.Application.Common.Interfaces;

/// <summary>
/// Issues short-lived signed access tokens carrying the claims
/// <c>sub</c> (UserId), <c>email</c>, <c>role</c> and <c>tenant_id</c>
/// (spec 5.1).
/// </summary>
public interface IAccessTokenService
{
    string CreateAccessToken(User user);
}
