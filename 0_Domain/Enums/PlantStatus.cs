using NpgsqlTypes;
using System.ComponentModel.DataAnnotations;

namespace ArandanoIRT.Web._0_Domain.Enums;

/// <summary>
/// Define los posibles estados de estrés hídrico de una planta.
/// </summary>
public enum PlantStatus
{
    /// <summary>
    /// La planta se encuentra en condiciones hídricas ideales.
    /// </summary>
    [PgName("OPTIMAL")]
    [Display(Name = "Óptimo")]
    OPTIMAL,

    /// <summary>
    /// La planta muestra signos iniciales de estrés hídrico.
    /// </summary>
    [PgName("MILD_STRESS")]
    [Display(Name = "Estrés Leve")]
    MILD_STRESS,

    /// <summary>
    /// La planta presenta un nivel de estrés hídrico crítico que requiere atención inmediata.
    /// </summary>
    [PgName("SEVERE_STRESS")]
    [Display(Name = "Estrés Severo")]
    SEVERE_STRESS,

    /// <summary>
    /// La planta está volviendo a un estado óptimo después de un período de estrés.
    /// </summary>
    [PgName("RECOVERING")]
    [Display(Name = "En Recuperación")]
    RECOVERING,

    /// <summary>
    /// El estado de la planta no puede determinarse debido a falta de datos o condiciones anómalas.
    /// </summary>
    [PgName("UNKNOWN")]
    [Display(Name = "Desconocido")]
    UNKNOWN
}