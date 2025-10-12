using System.Linq.Expressions;
using System.Security.Claims;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para el servicio de gestión de usuarios.
///     Es responsable de todas las operaciones relacionadas con el ciclo de vida de los usuarios,
///     incluyendo registro, autenticación, gestión de perfiles y acciones administrativas.
/// </summary>
public interface IUserService
{
    /// <summary>
    ///     Obtiene una lista de todos los usuarios para usar en un control de selección (dropdown).
    /// </summary>
    /// <returns>Una colección de <c>SelectListItem</c>.</returns>
    Task<IEnumerable<SelectListItem>> GetUsersForSelectionAsync();

    /// <summary>
    ///     Registra un nuevo usuario en el sistema utilizando un código de invitación.
    /// </summary>
    /// <param name="model">El DTO con los datos de registro del nuevo usuario.</param>
    /// <returns>Un objeto <c>Result</c> que indica si el registro fue exitoso.</returns>
    Task<Result> RegisterUserAsync(RegisterDto model);

    /// <summary>
    ///     Intenta autenticar a un usuario con sus credenciales.
    /// </summary>
    /// <param name="model">El DTO con el correo y la contraseña del usuario.</param>
    /// <returns>
    ///     Una tupla que contiene el resultado del inicio de sesión (<c>SignInResult</c>) y un booleano que indica si la
    ///     cuenta acaba de ser bloqueada.
    /// </returns>
    Task<(SignInResult Result, bool JustLockedOut)> LoginUserAsync(LoginDto model);

    /// <summary>
    ///     Genera un token de restablecimiento de contraseña y el enlace correspondiente.
    /// </summary>
    /// <param name="model">El DTO que contiene el correo del usuario.</param>
    /// <param name="urlHelper">Helper para generar URLs absolutas.</param>
    /// <param name="scheme">El esquema de la petición (http o https).</param>
    /// <returns>Un objeto <c>Result</c> que contiene el nombre del usuario y el enlace de restablecimiento.</returns>
    Task<Result<(string Name, string ResetLink)>> GeneratePasswordResetAsync(ForgotPasswordDto model,
        IUrlHelper urlHelper, string scheme);

    /// <summary>
    ///     Restablece la contraseña de un usuario utilizando un token de validación.
    /// </summary>
    /// <param name="model">El DTO con el token y la nueva contraseña.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la operación fue exitosa.</returns>
    Task<Result> ResetPasswordAsync(ResetPasswordDto model);

    /// <summary>
    ///     Permite a un usuario autenticado cambiar su propia contraseña.
    /// </summary>
    /// <param name="userPrincipal">El ClaimsPrincipal del usuario autenticado.</param>
    /// <param name="model">El DTO con la contraseña antigua y la nueva.</param>
    /// <returns>Un objeto <c>Result</c> que indica si el cambio fue exitoso.</returns>
    Task<Result> ChangePasswordAsync(ClaimsPrincipal userPrincipal, ChangePasswordDto model);

    /// <summary>
    ///     Actualiza la información del perfil de un usuario autenticado.
    /// </summary>
    /// <param name="userPrincipal">El ClaimsPrincipal del usuario autenticado.</param>
    /// <param name="model">El DTO con la nueva información del perfil.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la actualización fue exitosa.</returns>
    Task<Result> UpdateProfileAsync(ClaimsPrincipal userPrincipal, ProfileInfoDto model);

    /// <summary>
    ///     Obtiene una lista de administradores que han activado una notificación específica en su configuración.
    /// </summary>
    /// <param name="predicate">La condición que deben cumplir las configuraciones de la cuenta (ej. recibir alertas de ayuda).</param>
    /// <returns>Una lista de entidades <c>User</c> que son administradores y cumplen con el predicado.</returns>
    Task<List<User>> GetAdminsToNotifyAsync(Expression<Func<AccountSettings, bool>> predicate);

    /// <summary>
    ///     Obtiene una lista de todas las entidades de usuario.
    /// </summary>
    /// <returns>Una lista de todas las entidades <c>User</c>.</returns>
    Task<List<User>> GetAllUsersAsync();

    /// <summary>
    ///     Obtiene una lista de todos los usuarios formateada para la vista de gestión de usuarios.
    /// </summary>
    /// <returns>Un objeto <c>Result</c> que contiene una colección de <c>UserDto</c>.</returns>
    Task<Result<IEnumerable<UserDto>>> GetAllUsersForManagementAsync();

    /// <summary>
    ///     Promueve un usuario normal al rol de Administrador.
    /// </summary>
    /// <param name="userIdToPromote">El ID del usuario a promover.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la promoción fue exitosa.</returns>
    Task<Result> PromoteToAdminAsync(int userIdToPromote);

    /// <summary>
    ///     Elimina la cuenta de un usuario.
    /// </summary>
    /// <param name="userIdToDelete">El ID del usuario a eliminar.</param>
    /// <param name="currentUserId">El ID del usuario que realiza la operación (para validación).</param>
    /// <returns>Un objeto <c>Result</c> que indica si la eliminación fue exitosa.</returns>
    Task<Result> DeleteUserAsync(int userIdToDelete, int currentUserId);

    /// <summary>
    ///     Inicia el proceso de eliminación segura de una cuenta de administrador, que requiere confirmación.
    /// </summary>
    /// <param name="adminToDeleteId">El ID de la cuenta de administrador a eliminar.</param>
    /// <param name="currentAdminId">El ID del administrador que inicia el proceso.</param>
    /// <param name="currentAdminPassword">La contraseña del administrador que inicia el proceso para verificación.</param>
    /// <param name="urlHelper">Helper para generar URLs absolutas.</param>
    /// <param name="scheme">El esquema de la petición (http o https).</param>
    /// <returns>Un objeto <c>Result</c> que contiene el nombre del administrador a eliminar.</returns>
    Task<Result<string>> InitiateAdminDeletionAsync(int adminToDeleteId, int currentAdminId,
        string currentAdminPassword, IUrlHelper urlHelper, string scheme);

    /// <summary>
    ///     Confirma y ejecuta la eliminación de una cuenta de administrador utilizando un token de seguridad.
    /// </summary>
    /// <param name="adminToDeleteId">El ID de la cuenta de administrador a eliminar.</param>
    /// <param name="token">El token de confirmación recibido por correo.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la eliminación fue exitosa.</returns>
    Task<Result> ConfirmAdminDeletionAsync(int adminToDeleteId, string token);
}