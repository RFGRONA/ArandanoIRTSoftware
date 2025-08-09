using System.ComponentModel.DataAnnotations;
using NpgsqlTypes;

namespace ArandanoIRT.Web._0_Domain.Enums;

/// <summary>
///     Defines the experimental role of a plant within a study.
/// </summary>
public enum ExperimentalGroupType
{
    [PgName("MONITORED")] 
    [Display(Name = "Monitoreado")]
    MONITORED,
    
    [PgName("CONTROL")] 
    [Display(Name = "Controlado")]
    CONTROL,
    
    [PgName("STRESS")] 
    [Display(Name = "Estresado")]
    STRESS
}