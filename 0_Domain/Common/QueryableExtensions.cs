using System.Linq.Expressions;
using ArandanoIRT.Web._1_Application.Services.Contracts;

namespace ArandanoIRT.Web._0_Domain.Common;

public static class QueryableExtensions
{
    /// <summary>
    /// Aplica filtros de fecha de inicio y fin a una consulta IQueryable de forma genérica y segura.
    /// </summary>
    /// <typeparam name="T">El tipo de la entidad en el IQueryable.</typeparam>
    /// <param name="query">La consulta a la que se aplicarán los filtros.</param>
    /// <param name="filters">El objeto de filtros que contiene las fechas.</param>
    /// <param name="dateSelector">Una expresión lambda para seleccionar la propiedad de fecha de la entidad (ej. e => e.RecordedAtServer).</param>
    /// <returns>La consulta IQueryable con los filtros de fecha aplicados.</returns>
    public static IQueryable<T> ApplyDateFilters<T>(
        this IQueryable<T> query,
        DataQueryFilters filters,
        Expression<Func<T, DateTime>> dateSelector)
    {
        // Reutilizamos el parámetro de la expresión (ej. 'e')
        var parameter = dateSelector.Parameters[0];

        if (filters.StartDate.HasValue)
        {
            var startDateUtc = filters.StartDate.Value.ToUniversalTime();

            // Creamos dinámicamente la expresión: e.Fecha >= startDateUtc
            var startDateBody = Expression.GreaterThanOrEqual(dateSelector.Body, Expression.Constant(startDateUtc));
            var startDateLambda = Expression.Lambda<Func<T, bool>>(startDateBody, parameter);

            query = query.Where(startDateLambda);
        }

        if (filters.EndDate.HasValue)
        {
            // La lógica incluye todo el día de la fecha de fin.
            var endDate = filters.EndDate.Value.Date.AddDays(1);
            var endDateUtc = endDate.ToUniversalTime();

            // Creamos dinámicamente la expresión: e.Fecha < endDateUtc
            var endDateBody = Expression.LessThan(dateSelector.Body, Expression.Constant(endDateUtc));
            var endDateLambda = Expression.Lambda<Func<T, bool>>(endDateBody, parameter);

            query = query.Where(endDateLambda);
        }

        return query;
    }
}