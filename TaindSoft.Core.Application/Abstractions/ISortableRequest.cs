namespace TaindSoft.Core.Application.Abstractions
{
    /// <summary>
    /// TODO: Document interface ISortableRequest
    /// </summary>
    public interface ISortableRequest
    {
        string? SortBy { get; }
        bool Desc { get; }
    }
}
