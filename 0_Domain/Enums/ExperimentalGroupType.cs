using System.ComponentModel.DataAnnotations;
using NpgsqlTypes;

namespace ArandanoIRT.Web._0_Domain.Enums;

/// <summary>
/// Define el rol experimental de una planta dentro de un estudio o monitoreo.
/// </summary>
public enum ExperimentalGroupType
{
    /// <summary>
    /// Planta que es monitoreada activamente por el sistema.
    /// </summary>
    [PgName("MONITORED")]
    [Display(Name = "Monitoreado")]
    MONITORED,

    /// <summary>
    /// Planta utilizada como referencia, a la que se le aplican condiciones controladas y conocidas.
    /// </summary>
    [PgName("CONTROL")]
    [Display(Name = "Controlado")]
    CONTROL,

    /// <summary>
    /// Planta a la que se le induce un estrés hídrico de forma deliberada para calibrar o validar el sistema.
    /// </summary>
    [PgName("STRESS")]
    [Display(Name = "Estresado")]
    STRESS
}