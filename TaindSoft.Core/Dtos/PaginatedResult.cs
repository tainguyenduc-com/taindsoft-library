namespace TaindSoft.Core.Dtos
{
    /// <summary>
    /// Generic paginated result returned by list queries.
    /// </summary>
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

        public PaginatedResult()
        {
        }


        public PaginatedResult(IEnumerable<T> items, int totalCount, int page, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
        }

    }

    /// <summary>
    /// TODO: Document class PaginateResultExtensions
    /// </summary>
    public static class PaginateResultExtensions
    {
        public static PaginatedResult<T> Paginated<T>(this IQueryable<T> query, int page, int pageSize)
        {
            int totalCount = query.Count();
            IQueryable<T> result = query.Skip((page - 1) * pageSize).Take(pageSize);
            return new PaginatedResult<T>(result, totalCount, page, pageSize);
        }
    }
}
