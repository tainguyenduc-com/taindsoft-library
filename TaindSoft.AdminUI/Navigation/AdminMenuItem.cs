namespace TaindSoft.AdminUI.Navigation
{
    /// <summary>
    /// Represents a navigation menu item in the admin interface
    /// </summary>
    /// <summary>
    /// Represents a single item in the Admin navigation/menu structure.
    /// </summary>
    public class AdminMenuItem
    {
        /// <summary>
        /// Unique identifier for the menu item
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display label
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Navigation URL/route
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Icon (CSS class or emoji)
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Menu section grouping (null/empty = top level, outside sections)
        /// </summary>
        public string? Section { get; set; }

        /// <summary>
        /// Display order within section (lower value = higher priority)
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Global priority across all sections (lower value = displayed first)
        /// Used to ensure Dashboard always appears first before any section
        /// Default: 1000 (normal items), use 0-99 for special items like Dashboard
        /// </summary>
        public int Priority { get; set; } = 1000;

        /// <summary>
        /// Parent menu item ID for hierarchical menus
        /// </summary>
        public string? ParentId { get; set; }
    }
}
