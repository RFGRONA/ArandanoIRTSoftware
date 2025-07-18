using NpgsqlTypes;

namespace ArandanoIRT.Web._0_Domain.Enums;

public enum ActivationStatus
{
    [PgName("PENDING")]
    PENDING,

    [PgName("COMPLETED")]
    COMPLETED,

    [PgName("EXPIRED")]
    EXPIRED
}