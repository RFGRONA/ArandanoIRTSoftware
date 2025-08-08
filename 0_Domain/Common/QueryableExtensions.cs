using System.Linq.Expressions;
using ArandanoIRT.Web._1_Application.Services.Contracts;

namespace ArandanoIRT.Web._0_Domain.Common;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyDateFilters<T>(
        this IQueryable<T> query,
        DataQueryFilters filters,
        Expression<Func<T, DateTime>> dateSelector)
    {
        var parameter = dateSelector.Parameters[0];
        var colombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

        if (filters.StartDate.HasValue)
        {
            // Trata la fecha de inicio como el comienzo del día en Colombia y conviértela a UTC
            var startDateUnspecified = filters.StartDate.Value.Date;
            var startDateUtc = TimeZoneInfo.ConvertTimeToUtc(startDateUnspecified, colombiaTimeZone);

            var startDateBody = Expression.GreaterThanOrEqual(dateSelector.Body, Expression.Constant(startDateUtc));
            var startDateLambda = Expression.Lambda<Func<T, bool>>(startDateBody, parameter);
            query = query.Where(startDateLambda);
        }

        if (filters.EndDate.HasValue)
        {
            // Trata la fecha de fin como el final del día en Colombia y conviértela a UTC
            var endDateUnspecified = filters.EndDate.Value.Date.AddDays(1);
            var endDateUtc = TimeZoneInfo.ConvertTimeToUtc(endDateUnspecified, colombiaTimeZone);

            var endDateBody = Expression.LessThan(dateSelector.Body, Expression.Constant(endDateUtc));
            var endDateLambda = Expression.Lambda<Func<T, bool>>(endDateBody, parameter);
            query = query.Where(endDateLambda);
        }

        return query;
    }
}