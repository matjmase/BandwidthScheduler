using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using System.Text;

namespace BandwidthScheduler.Server.Common.Authentication
{
    public class PasswordSecurity
    {
        private static string GenerateRandomString(int length)
        {
            var randBytes = new byte[Base64RoundUp(length)];
            RandomNumberGenerator.Fill(randBytes);

            return Convert.ToBase64String(randBytes);
        }

        private static int Base64RoundUp(int length)
        {
            return (length + 3) / 4 * 3;
        }

        public static string GetSalt()
        {
            return GenerateRandomString(52);
        }

        public static string GetToken()
        {
            return GenerateRandomString(88);
        }

        public static string HashPassword(string pass, string salt)
        {
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: pass,
                salt: Encoding.ASCII.GetBytes(salt),
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: 10000,
                numBytesRequested: Base64RoundUp(88)));
        }
    }
}
