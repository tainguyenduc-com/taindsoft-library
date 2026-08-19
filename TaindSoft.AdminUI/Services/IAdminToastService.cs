namespace TaindSoft.AdminUI.Services
{
    /// <summary>
    /// TODO: Document interface IAdminToastService
    /// </summary>
    public interface IAdminToastService
    {
        event Action<ToastModel>? OnToastAdded;
        Task ShowAsync(string message, string? actionLabel = null, string? actionUrl = null, int durationSeconds = 7);
    }
}
