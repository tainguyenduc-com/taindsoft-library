namespace TaindSoft.AdminUI.Services
{
    /// <summary>
    /// TODO: Document class ToastModel
    /// </summary>
    public class ToastModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Message { get; set; } = string.Empty;
        public string? ActionLabel { get; set; }
        public string? ActionUrl { get; set; }
        public int DurationSeconds { get; set; } = 7;
        public ToastSeverity Severity { get; set; } = ToastSeverity.Success;
        // UI flag for enter/exit animation
        public bool Visible { get; set; } = false;
    }

    /// <summary>
    /// TODO: Document enum ToastSeverity
    /// </summary>
    public enum ToastSeverity
    {
        Success,
        Info,
        Warning,
        Error
    }
}
