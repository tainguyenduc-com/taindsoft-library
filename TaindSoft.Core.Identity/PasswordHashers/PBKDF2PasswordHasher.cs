using System.Security.Cryptography;

namespace TaindSoft.Core.Identity.PasswordHashers
{
    /// <summary>
    /// TODO: Document class PBKDF2PasswordHasher
    /// </summary>
    public class PBKDF2PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public string Hash(string password)
        {
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();

            byte[] salt = new byte[SaltSize];

            rng.GetBytes(salt);

            byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

            // Format: iterations.saltBase64.keyBase64
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public bool Verify(string password, string hashedPassword)
        {
            try
            {
                string[] parts = hashedPassword.Split('.', 3);
                if (parts.Length != 3)
                {
                    return false;
                }

                int iterations = int.Parse(parts[0]);
                byte[] salt = Convert.FromBase64String(parts[1]);
                byte[] key = Convert.FromBase64String(parts[2]);

                byte[] candidate = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, key.Length);

                return CryptographicOperations.FixedTimeEquals(candidate, key);
            }
            catch
            {
                return false;
            }
        }
    }
}
