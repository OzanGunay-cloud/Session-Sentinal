using System.Security.Cryptography;
using System.Text;
using SessionSentinel.Application.Abstractions;

namespace SessionSentinel.Infrastructure.Services;

public sealed class Sha256TokenHasher : ITokenHasher
{
    public string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
