using Microsoft.AspNetCore.Components;

namespace TaindSoft.AdminUI.Components.Shared;

/// <summary>
/// A single item inside <see cref="AdminCommandMenu"/>.
/// </summary>
public sealed class AdminCommandItem
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Icon { get; set; }
    public string? Shortcut { get; set; }
    public EventCallback OnSelect { get; set; }
}
