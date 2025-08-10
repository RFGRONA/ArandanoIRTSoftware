using System.ComponentModel.DataAnnotations;
using NpgsqlTypes;

namespace ArandanoIRT.Web._0_Domain.Enums;

public enum DeviceStatus
{
    [PgName("PENDING_ACTIVATION")]
    [Display(Name = "Pendiente")]
    PENDING_ACTIVATION,

    [PgName("ACTIVE")]
    [Display(Name = "Activo")]
    ACTIVE,

    [PgName("INACTIVE")]
    [Display(Name = "Inactivo")]
    INACTIVE,

    [PgName("MAINTENANCE")]
    [Display(Name = "Mantenimiento")]
    MAINTENANCE
}