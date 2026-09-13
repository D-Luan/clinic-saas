namespace HealthBr.Application.Common.Interfaces;

/// <summary>
/// Password hashing abstraction. Implemented with the ASP.NET Identity
/// PBKDF2 hasher only (spec 3.2) — no stores, no UserManager.
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password);

    /// <summary>Returns false for any result other than success.</summary>
    bool VerifyHashedPassword(string hashedPassword, string providedPassword);
}
