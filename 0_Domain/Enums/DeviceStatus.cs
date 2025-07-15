using NpgsqlTypes;

namespace ArandanoIRT.Web._0_Domain.Enums;

public enum DeviceStatus
{
    [PgName("PENDING_ACTIVATION")]
    PENDING_ACTIVATION,
    
    [PgName("ACTIVE")]
    ACTIVE,
    
    [PgName("INACTIVE")]
    INACTIVE,
    
    [PgName("MAINTENANCE")]
    MAINTENANCE
}