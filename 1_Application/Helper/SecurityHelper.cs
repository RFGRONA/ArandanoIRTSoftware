using System.Security.Cryptography;
using System.Text;

namespace ArandanoIRT.Web._1_Application.Helper;

public static class SecurityHelper
{
    public static string GenerateInvitationHash(string publicCode, string email)
    {
        // Normalizamos el correo a minúsculas para evitar problemas de mayúsculas/minúsculas
        var normalizedEmail = email.ToLowerInvariant();
        var stringToHash = $"{publicCode}:{normalizedEmail}";

        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(stringToHash);
            var hashBytes = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}