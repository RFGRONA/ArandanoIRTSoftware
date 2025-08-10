using NpgsqlTypes;
using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._0_Domain.Enums;

public enum PlantStatus
{
    [PgName("OPTIMAL")]
    [Display(Name = "Óptimo")]
    OPTIMAL,

    [PgName("MILD_STRESS")]
    [Display(Name = "Estrés Leve")]
    MILD_STRESS,

    [PgName("SEVERE_STRESS")]
    [Display(Name = "Estrés Severo")]
    SEVERE_STRESS,

    [PgName("RECOVERING")]
    [Display(Name = "En Recuperación")]
    RECOVERING,

    [PgName("UNKNOWN")]
    [Display(Name = "Desconocido")]
    UNKNOWN
}