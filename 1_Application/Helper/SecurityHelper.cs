using System.Security.Cryptography;
using System.Text;

namespace ArandanoIRT.Web._1_Application.Helper;

/// <summary>
///     Proporciona métodos de utilidad estáticos para operaciones relacionadas con la seguridad.
/// </summary>
public static class SecurityHelper
{
    /// <summary>
    ///     Genera un hash SHA256 único combinando un código público de invitación y el correo electrónico del invitado.
    ///     Esto permite crear un enlace de registro seguro y no adivinable.
    /// </summary>
    /// <param name="publicCode">El código de invitación visible.</param>
    /// <param name="email">El correo electrónico del usuario a invitar.</param>
    /// <returns>Una cadena hexadecimal que representa el hash SHA256.</returns>
    public static string GenerateInvitationHash(string publicCode, string email)
    {
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