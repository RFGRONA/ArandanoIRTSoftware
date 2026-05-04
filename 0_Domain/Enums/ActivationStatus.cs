using System.ComponentModel.DataAnnotations;
using NpgsqlTypes;

namespace ArandanoIRT.Web._0_Domain.Enums;

/// <summary>
/// Define los posibles estados de un código de activación de dispositivo.
/// </summary>
public enum ActivationStatus
{
    /// <summary>
    /// El código de activación ha sido generado pero aún no se ha utilizado.
    /// </summary>
    [PgName("PENDING")]
    [Display(Name = "Pendiente")]
    PENDING,

    /// <summary>
    /// El código de activación ya ha sido utilizado exitosamente para registrar un dispositivo.
    /// </summary>
    [PgName("COMPLETED")]
    [Display(Name = "Completado")]
    COMPLETED,

    /// <summary>
    /// El código de activación ha superado su fecha de validez y ya no puede ser utilizado.
    /// </summary>
    [PgName("EXPIRED")]
    [Display(Name = "Expirado")]
    EXPIRED
}