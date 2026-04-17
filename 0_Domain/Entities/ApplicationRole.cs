using Microsoft.AspNetCore.Identity;

namespace ArandanoIRT.Web._0_Domain.Entities;

/// <summary>
/// Representa un rol dentro del sistema de autenticación y autorización de la aplicación,
/// utilizando la infraestructura de ASP.NET Core Identity.
/// </summary>
public class ApplicationRole : IdentityRole<int>
{
}