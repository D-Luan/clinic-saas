using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace HealthBr.Application.Common.Security;

/// <summary>
/// Generates refresh tokens and computes the value persisted in the
/// database. Tokens are 256-bit random values encoded as base64url. Only the
/// SHA-256 hash is stored: because the token is high-entropy random (unlike a
/// user-chosen password), SHA-256 is a sufficient, collision-resistant lookup
/// key with no brute-force surface — slow password hashing (PBKDF2) stays
/// reserved for passwords (spec 3.2/15.3 A02).
/// </summary>
public static class RefreshTokenGenerator
{
    public static string Generate() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
