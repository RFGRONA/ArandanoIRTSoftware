using NpgsqlTypes;

namespace ArandanoIRT.Web._0_Domain.Enums;

public enum TokenStatus
{
    [PgName("ACTIVE")]
    ACTIVE,

    [PgName("REVOKED")]
    REVOKED
}