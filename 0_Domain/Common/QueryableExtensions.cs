using System.Linq.Expressions;
using ArandanoIRT.Web._1_Application.Services.Contracts;

namespace ArandanoIRT.Web._0_Domain.Common
{
    public static class QueryableExtensions
    {
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