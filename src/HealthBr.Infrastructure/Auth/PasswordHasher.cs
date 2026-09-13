using HealthBr.Application.Common.Interfaces;

using IdentityHasher = Microsoft.AspNetCore.Identity.PasswordHasher<object>;

namespace HealthBr.Infrastructure.Auth;

/// <summary>
/// PBKDF2 hashing via the ASP.NET Identity hasher only (spec 3.2) — no
/// stores, no UserManager.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly IdentityHasher _hasher = new();

    public string HashPassword(string password) => _hasher.HashPassword(new object(), password);

    public bool VerifyHashedPassword(string hashedPassword, string providedPassword) =>
        _hasher.VerifyHashedPassword(new object(), hashedPassword, providedPassword)
            != Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed;
}
