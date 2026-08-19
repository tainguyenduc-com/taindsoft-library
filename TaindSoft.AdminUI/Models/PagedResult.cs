namespace TaindSoft.AdminUI.Models
{
    public record PagedResult<T>
    {
        public List<T> Items { get; init; } = [];
        public int TotalCount { get; init; }
        public int PageSize { get; init; }
        public int CurrentPage { get; init; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
