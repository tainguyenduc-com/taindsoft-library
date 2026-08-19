using MudBlazor;

namespace TaindSoft.Core.MudBlazor
{
    public static class TaindSoftTheme
    {
        public static MudTheme Default => new MudTheme()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = "#2774AE",
                Secondary = "#3F88C5",
                Background = "#F7F9FB",
                Surface = "#FFFFFF",
                AppbarBackground = "#2774AE",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#323232",
                Success = "#009C87",
                Warning = "#FFA726",
                Error = "#E53935",
                TextPrimary = "#1B1F23",
                TextSecondary = "#323232A0"
            },
            PaletteDark = new PaletteDark()
            {
                Primary = "#2774AE",
                Secondary = "#3F88C5",
                Background = "#121212",
                Surface = "#1E1E1E",
                AppbarBackground = "#2774AE",
                DrawerBackground = "#1E1E1E",
                DrawerText = "#FFFFFF",
                Success = "#009C87",
                Warning = "#FFA726",
                Error = "#E53935",
                TextPrimary = "#F5F5F5",
                TextSecondary = "#AAAAAA"
            }
        };
    }
}