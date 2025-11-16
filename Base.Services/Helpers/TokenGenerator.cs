using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Helpers
{
    public static class TokenGenerator
    {
        public static string GenerateRandomToken(int byteSize = 64)
        {
            var bytes = new byte[byteSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public static string ComputeSha256Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash); // .NET 5+; use BitConverter for older
        }
    }
}
