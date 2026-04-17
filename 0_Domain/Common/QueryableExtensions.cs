using System.Linq.Expressions;
using ArandanoIRT.Web._1_Application.Services.Contracts;

namespace ArandanoIRT.Web._0_Domain.Common
{
/// <summary>
/// Proporciona métodos de extensión para la interfaz IQueryable,
/// facilitando la construcción de consultas dinámicas.
/// </summary>
    public static class QueryableExtensions
    {
    /// <summary>
    /// Aplica filtros de fecha de inicio y fin a una consulta IQueryable.
    /// La consulta se modifica para incluir solo los elementos que se encuentran
    /// dentro del rango de fechas especificado.
    /// </summary>
    /// <typeparam name="T">El tipo de los elementos en la consulta.</typeparam>
    /// <param name="query">La consulta IQueryable original.</param>
    /// <param name="filters">Los filtros que contienen las fechas de inicio y fin.</param>
    /// <param name="dateSelector">Una expresión para seleccionar la propiedad de fecha en el tipo T.</param>
    /// <returns>Una nueva consulta IQueryable con los filtros de fecha aplicados.</returns>
        public static IQueryable<T> ApplyDateFilters<T>(
            this IQueryable<T> query,
            DataQueryFilters filters,
            Expression<Func<T, DateTime>> dateSelector)
        {

            if (filters.StartDate.HasValue)
            {
                var startDateUtc = filters.StartDate.Value;

                var startPredicate = Expression.Lambda<Func<T, bool>>(
                    Expression.GreaterThanOrEqual(dateSelector.Body, Expression.Constant(startDateUtc)),
                    dateSelector.Parameters
                );
                query = query.Where(startPredicate);
            }

            if (filters.EndDate.HasValue)
            {
                var endDateUtc = filters.EndDate.Value;

                var endPredicate = Expression.Lambda<Func<T, bool>>(
                    Expression.LessThanOrEqual(dateSelector.Body, Expression.Constant(endDateUtc)),
                    dateSelector.Parameters
                );
                query = query.Where(endPredicate);
            }

            return query;
        }
    }
}