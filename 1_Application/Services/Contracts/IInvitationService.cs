using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para el servicio de gestión de invitaciones.
///     Es responsable de crear, validar y anular los códigos de invitación para el registro de nuevos usuarios.
/// </summary>
public interface IInvitationService
{
    /// <summary>
    ///     Crea un nuevo código de invitación para un correo electrónico específico.
    /// </summary>
    /// <param name="email">El correo electrónico del usuario a invitar.</param>
    /// <param name="isAdmin">Indica si la invitación es para una cuenta de administrador.</param>
    /// <param name="createdByUserId">El ID del usuario que crea la invitación (opcional).</param>
    /// <returns>Un objeto <c>Result</c> que contiene la entidad <c>InvitationCode</c> recién creada.</returns>
    Task<Result<InvitationCode>> CreateInvitationAsync(string email, bool isAdmin, int? createdByUserId);

    /// <summary>
    ///     Valida que un código de invitación sea correcto, corresponda al correo electrónico proporcionado y no haya
    ///     expirado.
    /// </summary>
    /// <param name="code">El código de invitación a validar.</param>
    /// <param name="email">El correo electrónico del usuario que intenta registrarse.</param>
    /// <returns>Un objeto <c>Result</c> que contiene la entidad <c>InvitationCode</c> si la validación es exitosa.</returns>
    Task<Result<InvitationCode>> ValidateCodeAsync(string code, string email);

    /// <summary>
    ///     Marca un código de invitación como utilizado para que no pueda ser reutilizado.
    /// </summary>
    /// <param name="invitationId">El ID del código de invitación a anular.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la operación fue exitosa.</returns>
    Task<Result> MarkCodeAsUsedAsync(int invitationId);
}