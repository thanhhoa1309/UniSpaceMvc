using Microsoft.EntityFrameworkCore;

namespace UniSpace.Service.Utils
{
    public static class PaginationExtensions
    {
        /// <summary>
        /// Creates a paginated result from an IQueryable source
        /// </summary>
        public static async Task<Pagination<T>> ToPaginationAsync<T>(
            this IQueryable<T> source,
            int pageNumber,
            int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new Pagination<T>(items, count, pageNumber, pageSize);
        }

        /// <summary>
        /// Creates a paginated result from a List
        /// </summary>
        public static Pagination<T> ToPagination<T>(
            this List<T> source,
            int pageNumber,
            int pageSize)
        {
            var count = source.Count;
            var items = source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new Pagination<T>(items, count, pageNumber, pageSize);
        }
    }
}
