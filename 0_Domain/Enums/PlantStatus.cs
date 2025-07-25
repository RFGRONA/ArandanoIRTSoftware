using NpgsqlTypes;

namespace ArandanoIRT.Web._0_Domain.Enums;

public enum PlantStatus
{
    [PgName("OPTIMAL")] OPTIMAL,

    [PgName("MILD_STRESS")] MILD_STRESS,

    [PgName("SEVERE_STRESS")] SEVERE_STRESS,

    [PgName("RECOVERING")] RECOVERING,

    [PgName("UNKNOWN")] UNKNOWN
}