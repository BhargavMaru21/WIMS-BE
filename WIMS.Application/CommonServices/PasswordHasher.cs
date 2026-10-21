using System.Security.Cryptography;
using System.Text;
using WIMS.Application.Interfaces.Common;

namespace WIMS.Application.CommonServices;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        string hash = BCrypt.Net.BCrypt.HashPassword(password);
        return hash;
    }

    public bool Verify(string password, string hash)
    {
        bool valid = BCrypt.Net.BCrypt.Verify(password, hash);
        return valid;
    }

    public string NormalHash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLower();
    }
}
