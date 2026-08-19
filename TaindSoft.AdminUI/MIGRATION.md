# shadcn-admin → Blazor AdminUI Migration Mapping

> **Migration Date:** 2026-07-09  
> **Scope:** UI primitives + Layout components only (no feature modules/pages)  
> **Approach:** Reuse TaindSoft.AdminUI conventions, extend with missing components, hardcode mock data inline

---

## Component Mapping Table

| shadcn-admin (React)              | TaindSoft.AdminUI (Blazor)                                | Status      | Notes                                                                 |
|-----------------------------------|-----------------------------------------------------------|-------------|-----------------------------------------------------------------------|
| `components/ui/button.tsx`        | `Components/ShadcnButton.razor`                            | ✅ Exists   | Already implemented; variant, size, icon support                      |
| `components/ui/card.tsx`          | `Components/Card/ShadcnCard.razor`                         | ✅ Exists   | ShadcnCard, ShadcnCardHeader, ShadcnCardContent, ShadcnCardFooter            |
| `components/ui/dialog.tsx`        | `Components/AdminModal.razor`                             | ✅ Exists   | Modal with title, body, footer; IsOpen binding                        |
| `components/ui/table.tsx`         | `Components/AdminTable.razor`                             | ✅ Exists   | Data table with pagination                                            |
| `components/ui/input.tsx`         | `Components/Shared/AdminInput.razor`                      | ✅ Exists   | Text input with label                                                 |
| `components/ui/select.tsx`        | `Components/Shared/AdminSelect.razor`                     | ✅ Exists   | Dropdown select with label                                            |
| `components/ui/textarea.tsx`      | `Components/Shared/AdminTextArea.razor`                   | ✅ Exists   | Multiline textarea                                                    |
| `components/ui/checkbox.tsx`      | `Components/Shared/AdminCheckbox.razor`                   | ✅ Exists   | Checkbox with label                                                   |
| `components/ui/badge.tsx`         | `Components/Shared/AdminBadge.razor`                      | ✅ Created  | Inline badge with variants (primary, secondary, success, etc.)        |
| `components/ui/avatar.tsx`        | `Components/Shared/AdminAvatar.razor`                     | ✅ Created  | Circular avatar with image or initials fallback                       |
| `components/ui/skeleton.tsx`      | `Components/Shared/AdminSkeleton.razor`                   | ✅ Created  | Shimmer placeholder for loading states                                |
| `components/ui/separator.tsx`     | `Components/Shared/AdminSeparator.razor`                  | ✅ Created  | Horizontal/vertical divider line                                      |
| `components/ui/tooltip.tsx`       | `Components/Shared/AdminTooltip.razor`                    | ✅ Created  | Hover tooltip (CSS-based, no JS)                                      |
| `components/ui/popover.tsx`       | `Components/Shared/AdminPopover.razor`                    | ✅ Created  | Click-triggered floating panel                                        |
| `components/ui/sheet.tsx`         | `Components/Shared/AdminSheet.razor`                      | ✅ Created  | Slide-over drawer (left/right/top/bottom)                             |
| `components/ui/tabs.tsx`          | `Components/AdminTabs.razor` + `AdminTab.razor`           | ✅ Created  | Tab navigation with cascading context                                 |
| `components/ui/switch.tsx`        | `Components/Shared/AdminSwitch.razor`                     | ✅ Created  | Toggle switch control                                                 |
| `components/ui/dropdown-menu.tsx` | `Components/Shared/AdminDropdown.razor`                   | ✅ Created  | Click-triggered dropdown menu                                         |
| `components/ui/calendar.tsx`      | `Components/Shared/AdminCalendar.razor`                   | ✅ Created  | Month picker with prev/next navigation                                |
| `components/command-menu.tsx`     | `Components/Shared/AdminCommandMenu.razor`                | ✅ Created  | Command palette modal (⌘K style)                                      |
| `components/theme-switch.tsx`     | `Layout/AdminThemeSwitch.razor`                           | ✅ Created  | Light/Dark/System theme switcher (localStorage persistence)           |
| `components/layout/team-switcher.tsx` | `Layout/AdminTeamSwitcher.razor`                      | ✅ Created  | Sidebar workspace/team dropdown                                       |
| `components/layout/nav.tsx`       | `Layout/AdminNav.razor`                                   | ✅ Created  | Sidebar nav wrapper (delegates to AdminMenu)                          |
| `components/layout/sidebar.tsx`   | `Layout/AdminSidebar.razor`                               | ✅ Exists   | Already implemented; collapsible sidebar                              |
| `components/layout/header.tsx`    | `Layout/AdminHeader.razor`                                | ✅ Exists   | Already implemented; top bar with search, theme toggle, user menu     |
| `components/layout/authenticated-layout.tsx` | `Layout/AdminLayout.razor`                     | ✅ Exists   | Already implemented; complete admin shell                             |

---

## New Components Summary

### UI Primitives (12 new)
- `AdminBadge` — inline badge with variants
- `AdminAvatar` — circular avatar with fallback
- `AdminSkeleton` — loading shimmer
- `AdminSeparator` — divider line
- `AdminTooltip` — hover tooltip
- `AdminPopover` — click popover
- `AdminSheet` — slide-over drawer
- `AdminTabs` / `AdminTab` — tab navigation
- `AdminSwitch` — toggle switch
- `AdminDropdown` — dropdown menu
- `AdminCalendar` — month picker
- `AdminCommandMenu` — command palette

### Layout Components (3 new)
- `AdminThemeSwitch` — light/dark/system theme switcher
- `AdminTeamSwitcher` — workspace/team dropdown
- `AdminNav` — sidebar nav wrapper

---

## Implementation Notes

### Technology Stack
- **CSS Framework:** Tailwind CSS (existing in TaindSoft.AdminUI)
- **Component Pattern:** Razor + partial class (.razor + .razor.cs)
- **Naming Convention:** Admin* prefix for all components
- **Icons:** HeroIcon component (inline SVG via string-switch)
- **Imports:** Auto-imported via `_Imports.razor`

### Parameter Patterns
- `[Parameter]` for all public props
- `[Parameter(CaptureUnmatchedValues = true)] IDictionary<string, object>? AdditionalAttributes` for pass-through attributes
- `EventCallback<T>` for two-way binding (e.g., `Value` + `ValueChanged`)
- `RenderFragment?` for child content slots

### State Management
- **Local state:** Component `@code` block fields (`_open`, `_current`, etc.)
- **Persistence:** `localStorage` via `IJSRuntime` (e.g., theme preference)
- **No global state:** Each component is self-contained

### Styling Approach
- Reuse existing Tailwind classes from TaindSoft.AdminUI
- No new CSS files added
- All variants handled via conditional Tailwind classes in `@code` methods

---

## What Was NOT Migrated

### Out of Scope (as per user selection)
- ❌ **Feature modules/pages** (Users, Tasks, Settings, Dashboard, Auth, Chats, Apps, Errors) — UI primitives + Layout only
- ❌ **Business logic / data** — all components use inline mock data or empty defaults
- ❌ **Typed HTTP clients** — no `IXxxApiService` implementations
- ❌ **State management services** — no Zustand-equivalent stores

### Why
User selected **"UI-only, hardcode mock data inline (Recommended for v1)"** — focus on component library foundation first, wire to real APIs later.

---

## File Locations

### UI Primitives
```
source/library/TaindSoft.AdminUI/Components/Shared/
├── AdminBadge.razor + .razor.cs
├── AdminAvatar.razor + .razor.cs
├── AdminSkeleton.razor + .razor.cs
├── AdminSeparator.razor + .razor.cs
├── AdminTooltip.razor + .razor.cs
├── AdminPopover.razor + .razor.cs
├── AdminSheet.razor + .razor.cs
├── AdminSwitch.razor + .razor.cs
├── AdminDropdown.razor + .razor.cs
├── AdminCalendar.razor + .razor.cs
├── AdminCommandMenu.razor + .razor.cs
└── AdminCommandItem.cs (data model)
```

### Tabs (special case: in Components/ root)
```
source/library/TaindSoft.AdminUI/Components/
├── AdminTabs.razor + .razor.cs
└── AdminTab.razor + .razor.cs
```

### Layout Components
```
source/library/TaindSoft.AdminUI/Layout/
├── AdminThemeSwitch.razor + .razor.cs
├── AdminTeamSwitcher.razor + .razor.cs
├── AdminTeamOption.cs (data model)
└── AdminNav.razor + .razor.cs
```

---

## Build Verification

```bash
dotnet build source/library/TaindSoft.AdminUI/TaindSoft.AdminUI.csproj --nologo -v q
```

**Result:** ✅ Build succeeded (0 errors, 0 warnings)

---

## Next Steps (Future Work)

1. **Demo pages** — Create demo pages in `Pages/Demos/` showcasing each new component with variants
2. **Integration** — Wire components to real APIs (e.g., `IContentApiService`)
3. **Feature modules** — Migrate shadcn-admin feature pages (Users, Tasks, Settings, Dashboard) to module Pages
4. **Testing** — Add unit tests for new components (xUnit + bUnit)
5. **Documentation** — Expand component API docs, usage examples, props reference

---

## References

- shadcn-admin source: `.theme/shadcn-admin/src/`
- TaindSoft.AdminUI library: `source/library/TaindSoft.AdminUI/`
- Skill: `taindsoft-admin-module` (IAdminModule pattern, DI, navigation)
- Tailwind CSS: https://tailwindcss.com/docs
- Blazor components: https://learn.microsoft.com/aspnet/core/blazor/components/

---

_Migration completed successfully. All UI primitives and Layout components are now available in TaindSoft.AdminUI._
