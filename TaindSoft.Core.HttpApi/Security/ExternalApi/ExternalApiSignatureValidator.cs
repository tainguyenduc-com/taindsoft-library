using System.Security.Cryptography;
using System.Text;

namespace TaindSoft.Core.HttpApi.Security.ExternalApi
{
    /// <summary>
    /// TODO: Document class ExternalApiSignatureValidator
    /// </summary>
    public static class ExternalApiSignatureValidator
    {
        public static string ComputeBodyHash(byte[] body)
        {
            if (body == null || body.Length == 0)
            {
                return Convert.ToBase64String(SHA256.HashData(Array.Empty<byte>()));
            }

            return Convert.ToBase64String(SHA256.HashData(body));
        }

        public static string BuildSignatureBase(string method, string path, string timestamp, string bodyHash)
        {
            return string.Concat(method.ToUpperInvariant(), "\n", path, "\n", timestamp, "\n", bodyHash);
        }

        public static byte[] ComputeHmacSha256(string secret, string signatureBase)
        {
            byte[] key = Encoding.UTF8.GetBytes(secret);
            byte[] data = Encoding.UTF8.GetBytes(signatureBase);
            using HMACSHA256 hmac = new(key);
            return hmac.ComputeHash(data);
        }

        public static bool VerifySignature(string expectedBase64, byte[] computed)
        {
            try
            {
                byte[] expected = Convert.FromBase64String(expectedBase64);
                return CryptographicOperations.FixedTimeEquals(expected, computed);
            }
            catch
            {
                return false;
            }
        }
    }
}
