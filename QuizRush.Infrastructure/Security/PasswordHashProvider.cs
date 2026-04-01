using System.Security.Cryptography;

namespace QuizRush.Infrastructure.Security;

public class PasswordHashProvider
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public static string GenerateSalt()
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        return Convert.ToBase64String(salt);
    }

    public static string HashPassword(string password, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);
        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
        return Convert.ToBase64String(hashBytes);
    }

    public static bool VerifyPassword(string password, string hash, string salt)
    {
        string computedHash = HashPassword(password, salt);
        return computedHash == hash;
    }
}
