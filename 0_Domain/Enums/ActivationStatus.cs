using System.ComponentModel.DataAnnotations;
using NpgsqlTypes;

namespace ArandanoIRT.Web._0_Domain.Enums;

public enum ActivationStatus
{
    [PgName("PENDING")]
    [Display(Name = "Pendiente")]
    PENDING,

    [PgName("COMPLETED")]
    [Display(Name = "Completado")]
    COMPLETED,

    [PgName("EXPIRED")]
    [Display(Name = "Expirado")]
    EXPIRED
}