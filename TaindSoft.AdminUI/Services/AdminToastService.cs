namespace TaindSoft.AdminUI.Services
{
    /// <summary>
    /// Provides toast/notification services for Admin UI components.
    /// </summary>
    public class AdminToastService : IAdminToastService
    {
        public event Action<ToastModel>? OnToastAdded;

        public Task ShowAsync(string message, string? actionLabel = null, string? actionUrl = null, int durationSeconds = 7)
        {
            ToastModel t = new()
            {
                Message = message,
                ActionLabel = actionLabel,
                ActionUrl = actionUrl,
                DurationSeconds = durationSeconds
            };
            OnToastAdded?.Invoke(t);
            return Task.CompletedTask;
        }
    }
}
