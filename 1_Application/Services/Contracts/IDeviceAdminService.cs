using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.DTOs.Common;
using ArandanoIRT.Web._1_Application.DTOs.Device;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web._1_Application.Services.Contracts;

// DTO para el resultado de la creación de un dispositivo, incluyendo el código de activación.
/// <summary>
///     DTO que encapsula el resultado exitoso de la creación de un dispositivo.
///     Contiene el ID del nuevo dispositivo y su código de activación.
/// </summary>
public class DeviceCreationResultDto
{
    public int DeviceId { get; set; }
    public string ActivationCode { get; set; } = string.Empty;
    public DateTime ActivationCodeExpiresAt { get; set; }
}

/// <summary>
///     Define el contrato para el servicio de administración de dispositivos.
///     Es responsable de todas las operaciones CRUD (Crear, Leer, Actualizar, Eliminar)
///     realizadas sobre los dispositivos desde el panel de administración web.
/// </summary>
public interface IDeviceAdminService
{
    /// <summary>
    ///     Obtiene una lista resumida de todos los dispositivos registrados.
    /// </summary>
    /// <returns>Un objeto <c>Result</c> que contiene una colección de <c>DeviceSummaryDto</c>.</returns>
    Task<Result<IEnumerable<DeviceSummaryDto>>> GetAllDevicesAsync();
    Task<Result<PagedResultDto<DeviceSummaryDto>>> GetPagedDevicesAsync(DeviceQueryFilters filters);
    /// <summary>
    ///     Obtiene los detalles completos de un dispositivo específico por su ID.
    /// </summary>
    /// <param name="deviceId">El ID del dispositivo a buscar.</param>
    /// <returns>Un objeto <c>Result</c> que contiene el <c>DeviceDetailsDto</c> o null si no se encuentra.</returns>
    Task<Result<DeviceDetailsDto?>> GetDeviceByIdAsync(int deviceId);
    /// <summary>
    ///     Obtiene los datos de un dispositivo, formateados para poblar un formulario de edición.
    /// </summary>
    /// <param name="deviceId">El ID del dispositivo a editar.</param>
    /// <returns>Un objeto <c>Result</c> que contiene el <c>DeviceEditDto</c> o null si no se encuentra.</returns>
    Task<Result<DeviceEditDto?>> GetDeviceForEditByIdAsync(int deviceId);
    /// <summary>
    ///     Crea un nuevo dispositivo y genera su código de activación inicial.
    /// </summary>
    /// <param name="deviceDto">El DTO con la información del nuevo dispositivo.</param>
    /// <returns>Un objeto <c>Result</c> que contiene el <c>DeviceCreationResultDto</c> con el ID y el código de activación.</returns>
    Task<Result<DeviceCreationResultDto>> CreateDeviceAsync(DeviceCreateDto deviceDto);
    /// <summary>
    ///     Actualiza la información de un dispositivo existente.
    /// </summary>
    /// <param name="deviceDto">El DTO con los datos actualizados del dispositivo.</param>
    /// <returns>Un objeto <c>Result</c> que indica si la operación fue exitosa.</returns>
    Task<Result> UpdateDeviceAsync(DeviceEditDto deviceDto);
    Task<Result> DeleteDeviceAsync(int deviceId); // Cuidado con las dependencias

    // Métodos para poblar dropdowns
    /// <summary>
    ///     Obtiene una lista de los posibles estados de un dispositivo para usar en un control de selección (dropdown).
    /// </summary>
    /// <returns>Una colección de <c>SelectListItem</c>.</returns>
    IEnumerable<SelectListItem> GetDeviceStatusesForSelection();
    /// <summary>
    ///     Obtiene una lista de todas las plantas para usar en un control de selección (dropdown).
    /// </summary>
    /// <returns>Una colección de <c>SelectListItem</c>.</returns>
    Task<IEnumerable<SelectListItem>> GetPlantsForSelectionAsync();
}