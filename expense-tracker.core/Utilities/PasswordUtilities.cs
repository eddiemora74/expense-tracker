using System.Security.Cryptography;
using System.Text;

namespace expense_tracker.core.Utilities;

public class PasswordUtilities
{
    public static string GenerateSalt(int size = 16)
    {
        var saltBytes = new byte[size];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        return Convert.ToBase64String(saltBytes);
    }

    public static string HashPassword(string password, string salt)
    {
        using var sha256 = SHA256.Create();
        // Combine password and salt
        var combined = Encoding.UTF8.GetBytes(password + salt);
        var hashBytes = sha256.ComputeHash(combined);

        // Convert hash to Base64 for storage
        return Convert.ToBase64String(hashBytes);
    }
    
    public static bool VerifyPassword(string enteredPassword, string storedSalt, string storedHash)
    {
        string hashOfEnteredPassword = HashPassword(enteredPassword, storedSalt);
        return hashOfEnteredPassword == storedHash;
    }
}