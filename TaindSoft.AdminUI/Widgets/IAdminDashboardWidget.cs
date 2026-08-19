using Microsoft.AspNetCore.Components;

namespace TaindSoft.AdminUI.Widgets
{
    /// <summary>
    /// Contract for dashboard widgets provided by modules
    /// </summary>
    public interface IAdminDashboardWidget
    {
        /// <summary>
        /// Unique widget identifier
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Widget title displayed in dashboard
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Display order
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Module that provides this widget
        /// </summary>
        string ModuleName { get; }

        /// <summary>
        /// Render the widget content
        /// </summary>
        RenderFragment Content { get; }
    }

    /// <summary>
    /// Service that provides dashboard widgets from modules
    /// </summary>
    public interface IAdminDashboardWidgetProvider
    {
        /// <summary>
        /// Get all widgets provided by this module
        /// </summary>
        Task<IEnumerable<IAdminDashboardWidget>> GetWidgetsAsync();
    }
}
