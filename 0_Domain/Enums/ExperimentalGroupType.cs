using NpgsqlTypes;

namespace ArandanoIRT.Web._0_Domain.Enums;

/// <summary>
///     Defines the experimental role of a plant within a study.
/// </summary>
public enum ExperimentalGroupType
{
    [PgName("MONITORED")] MONITORED,
    [PgName("CONTROL")] CONTROL,
    [PgName("STRESS")] STRESS
}