using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace SoundWords.Auth;

/// <summary>
/// Grandfathers password hashes that came from the ServiceStack auth tables.
/// PBKDF2 rows are wire-compatible with Identity v3 and are stored as-is, so the
/// stock hasher handles them. SaltedHash rows (HMAC-SHA-256 with a separate Salt
/// column) are stored prefixed with <see cref="LegacyPrefix"/> during migration;
/// on successful verification we report SuccessRehashNeeded so Identity rewrites
/// the hash in v3 format on the next login.
/// </summary>
public sealed class LegacyAwarePasswordHasher : IPasswordHasher<ApplicationUser>
{
    public const string LegacyPrefix = "SS$";
    private readonly PasswordHasher<ApplicationUser> _identity = new();

    public string HashPassword(ApplicationUser user, string password)
    {
        return _identity.HashPassword(user, password);
    }

    public PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword,
                                                          string providedPassword)
    {
        if (!hashedPassword.StartsWith(LegacyPrefix, StringComparison.Ordinal))
        {
            return _identity.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }

        string[] parts = hashedPassword[LegacyPrefix.Length..].Split('$', 2);
        if (parts.Length != 2)
        {
            return PasswordVerificationResult.Failed;
        }

        string saltB64 = parts[0];
        string expectedHashB64 = parts[1];

        byte[] saltBytes;
        byte[] expectedBytes;
        try
        {
            saltBytes = Convert.FromBase64String(saltB64);
            expectedBytes = Convert.FromBase64String(expectedHashB64);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }

        using HMACSHA256 hmac = new(saltBytes);
        byte[] computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(providedPassword));
        return CryptographicOperations.FixedTimeEquals(computed, expectedBytes)
                   ? PasswordVerificationResult.SuccessRehashNeeded
                   : PasswordVerificationResult.Failed;
    }
}
