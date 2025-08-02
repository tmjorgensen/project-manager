using Microsoft.EntityFrameworkCore;

namespace Api.Extensions;

public static class IQueryableExtensions
{
    public static IQueryable<T> OrderBy<T>(this IQueryable<T> query, string querySort)
    {
        if (string.IsNullOrWhiteSpace(querySort))
            return query; // Return the original queryable if no sort is specified  

        var sortparts = querySort.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (sortparts.Length == 2)
        {
            var property = sortparts[0];
            var direction = sortparts[1].ToLower();

            if (direction == "asc")
                return query.OrderBy(x => EF.Property<T>(x, property));
            else if (direction == "desc")
                return query.OrderByDescending(x => EF.Property<T>(x, property));
        }

        return query; // Default return if sortparts are invalid  
    }
}
