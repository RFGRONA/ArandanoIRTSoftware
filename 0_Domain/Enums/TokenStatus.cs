using System.ComponentModel.DataAnnotations;
using NpgsqlTypes;

namespace ArandanoIRT.Web._0_Domain.Enums;

public enum TokenStatus
{
    [PgName("ACTIVE")]
    [Display(Name = "Activo")]
    ACTIVE,

    [PgName("REVOKED")]
    [Display(Name = "Revocado")]
    REVOKED
}