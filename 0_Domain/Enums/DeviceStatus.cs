using System.ComponentModel.DataAnnotations;
using NpgsqlTypes;

namespace ArandanoIRT.Web._0_Domain.Enums;

/// <summary>
/// Representa el estado operativo de un dispositivo de hardware.
/// </summary>
public enum DeviceStatus
{
    /// <summary>
    /// El dispositivo ha sido registrado en la plataforma pero aún no ha sido activado con su código.
    /// </summary>
    [PgName("PENDING_ACTIVATION")]
    [Display(Name = "Pendiente")]
    PENDING_ACTIVATION,

    /// <summary>
    /// El dispositivo está activado y funcionando correctamente.
    /// </summary>
    [PgName("ACTIVE")]
    [Display(Name = "Activo")]
    ACTIVE,

    /// <summary>
    /// El dispositivo ha dejado de enviar datos por un período de tiempo predefinido.
    /// </summary>
    [PgName("INACTIVE")]
    [Display(Name = "Inactivo")]
    INACTIVE,

    /// <summary>
    /// El dispositivo ha sido puesto manualmente fuera de servicio para reparaciones o actualizaciones.
    /// </summary>
    [PgName("MAINTENANCE")]
    [Display(Name = "Mantenimiento")]
    MAINTENANCE
}