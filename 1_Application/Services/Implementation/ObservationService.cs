using System.Security.Claims;
using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.DTOs.Admin;
using ArandanoIRT.Web._1_Application.DTOs.Common;
using ArandanoIRT.Web._1_Application.DTOs.Observations;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._3_Presentation.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

/// <summary>
///     Implementación del servicio que gestiona las observaciones manuales de los agrónomos.
/// </summary>
public class ObservationService : IObservationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ObservationService> _logger;
    private readonly UserManager<User> _userManager;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="ObservationService" />.
    /// </summary>
    /// <param name="context">El contexto de la base de datos.</param>
    /// <param name="userManager">El servicio para la gestión de usuarios de ASP.NET Core Identity.</param>
    /// <param name="logger">El servicio de logging.</param>
    public ObservationService(ApplicationDbContext context, UserManager<User> userManager,
        ILogger<ObservationService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> CreateObservationAsync(ObservationCreateDto model, ClaimsPrincipal userPrincipal)
    {
        // Obtenemos el usuario completo a partir del ClaimsPrincipal de la sesión.
        var user = await _userManager.GetUserAsync(userPrincipal);
        if (user == null) return Result.Failure("No se pudo identificar al usuario autenticado.");

        try
        {
            var observation = new Observation
            {
                PlantId = model.PlantId,
                UserId = user.Id, // Asignamos el ID del usuario actual
                Description = model.Description,
                SubjectiveRating = model.SubjectiveRating,
                CreatedAt = DateTime.UtcNow
            };

            _context.Observations.Add(observation);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Nueva observación (ID: {ObservationId}) creada por el usuario {UserId} para la planta {PlantId}",
                observation.Id, user.Id, model.PlantId);

            return Result.Success(observation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar la nueva observación.");
            return Result.Failure("Error interno al guardar la observación.");
        }
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<ObservationListDto>> GetPagedObservationsAsync(ObservationQueryFilters filters)
    {
        var query = _context.Observations.AsNoTracking();

        // Aplicar filtros
        if (filters.PlantId.HasValue)
            query = query.Where(o => o.PlantId == filters.PlantId.Value);

        if (filters.UserId.HasValue)
            query = query.Where(o => o.UserId == filters.UserId.Value);

        if (filters.StartDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= filters.StartDate.Value);
        }
        if (filters.EndDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= filters.EndDate.Value);
        }

        // Contar el total de resultados ANTES de paginar
        var totalCount = await query.CountAsync();

        // Aplicar orden y paginación
        if (filters.SortOrder?.ToLower() == "asc")
            query = query.OrderBy(o => o.CreatedAt);
        else
            query = query.OrderByDescending(o => o.CreatedAt);

        var items = await query
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(o => new ObservationListDto
            {
                Id = o.Id,
                PlantName = o.Plant.Name,
                UserName = o.User.FirstName + " " + o.User.LastName,
                Description = o.Description,
                SubjectiveRating = o.SubjectiveRating,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return new PagedResultDto<ObservationListDto>
        {
            Items = items,
            PageNumber = filters.PageNumber,
            PageSize = filters.PageSize,
            TotalCount = totalCount
        };
    }
}