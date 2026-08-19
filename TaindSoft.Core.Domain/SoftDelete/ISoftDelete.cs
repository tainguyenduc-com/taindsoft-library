namespace TaindSoft.Core.Domain.SoftDelete
{
    /// <summary>
    /// Optional soft-delete marker interface.
    /// </summary>
    public interface ISoftDelete
    {
        bool IsDeleted { get; }
        DateTime? DeletedAt { get; }
        int DeletedBy { get; }
    }
}
