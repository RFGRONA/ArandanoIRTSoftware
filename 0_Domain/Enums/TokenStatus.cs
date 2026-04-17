using System.ComponentModel.DataAnnotations;
using NpgsqlTypes;

namespace ArandanoIRT.Web._0_Domain.Enums;

/// <summary>
/// Define el estado de un token de autenticación de dispositivo.
/// </summary>
public enum TokenStatus
{
    /// <summary>
    /// El token es válido y puede ser utilizado para la autenticación.
    /// </summary>
    [PgName("ACTIVE")]
    [Display(Name = "Activo")]
    ACTIVE,

    /// <summary>
    /// El token ha sido invalidado y ya no puede ser utilizado.
    /// </summary>
    [PgName("REVOKED")]
    [Display(Name = "Revocado")]
    REVOKED
}