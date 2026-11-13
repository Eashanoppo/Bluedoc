using System;
using System.Windows;
using System.Windows.Media;

namespace BlueDoc.UI.Common
{
    public enum AppTheme
    {
        Dark,
        Light
    }

    public static class ThemeManager
    {
        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;

        public static event Action<AppTheme> ThemeChanged;

        public static void SetTheme(AppTheme theme)
        {
            CurrentTheme = theme;
            var resources = System.Windows.Application.Current?.Resources;
            if (resources == null) return;

            if (theme == AppTheme.Light)
            {
                // Core Surfaces
                SetBrush(resources, "BgDarkBrush", "#F6F8FA");
                SetBrush(resources, "DarkBgBrush", "#F6F8FA");
                SetBrush(resources, "SurfaceCanvasBrush", "#F6F8FA");
                SetBrush(resources, "SidebarBgBrush", "#EEF1F4");
                SetBrush(resources, "SurfaceSidebarBrush", "#EEF1F4");
                SetBrush(resources, "CardBgBrush", "#FFFFFF");
                SetBrush(resources, "SurfaceCardBrush", "#FFFFFF");
                SetBrush(resources, "CardBgHoverBrush", "#F3F4F6");
                SetBrush(resources, "SurfaceCardHoverBrush", "#F3F4F6");
                SetBrush(resources, "CardBgPressedBrush", "#E5E7EB");
                SetBrush(resources, "SurfaceElevatedBrush", "#FFFFFF");
                SetBrush(resources, "SurfaceSunkenBrush", "#EAEEF2");

                // Borders
                SetBrush(resources, "BorderBrush", "#D0D7DE");
                SetBrush(resources, "BorderSubtleBrush", "#D0D7DE");
                SetBrush(resources, "BorderStrongBrush", "#AFB8C1");
                SetBrush(resources, "BorderFocusBrush", "#0969DA");
                SetBrush(resources, "FocusRingBrush", "#0969DA");

                // Typography / Text
                SetBrush(resources, "TextPrimaryBrush", "#1F2328");
                SetBrush(resources, "TextSecondaryBrush", "#57606A");
                SetBrush(resources, "TextMutedBrush", "#656D76");
                SetBrush(resources, "TextDisabledBrush", "#8C959F");
                SetBrush(resources, "TextOnAccentBrush", "#FFFFFF");

                // Brand / Accent
                SetBrush(resources, "AccentBrush", "#0969DA");
                SetBrush(resources, "AccentHoverBrush", "#0854B2");
                SetBrush(resources, "AccentPressedBrush", "#074592");
                SetBrush(resources, "AccentSubtleBrush", "#DDF4FF");

                // Semantics
                SetBrush(resources, "SuccessBrush", "#1A7F37");
                SetBrush(resources, "SuccessSubtleBrush", "#DAFBE1");
                SetBrush(resources, "WarningBrush", "#9A6700");
                SetBrush(resources, "WarningSubtleBrush", "#FFF8C5");
                SetBrush(resources, "DangerBrush", "#CF222E");
                SetBrush(resources, "DangerSubtleBrush", "#FFEBE9");
                SetBrush(resources, "InfoBrush", "#0969DA");
                SetBrush(resources, "InfoSubtleBrush", "#DDF4FF");

                // Navigation
                SetBrush(resources, "NavActiveBgBrush", "#E1E4E8");
                SetBrush(resources, "NavActiveTextBrush", "#0969DA");
                SetBrush(resources, "NavHoverBgBrush", "#EAEEF2");
                SetBrush(resources, "HeaderBgBrush", "#F6F8FA");
                SetBrush(resources, "NavGroupTextBrush", "#656D76");
                SetBrush(resources, "NavGroupBgBrush", "#F6F8FA");
                SetBrush(resources, "ArrowBrush", "#8C959F");
                SetBrush(resources, "ArrowActiveBrush", "#0969DA");

                // Segmented Switch
                SetBrush(resources, "PillBgBrush", "#E1E4E8");
                SetBrush(resources, "PillActiveBgBrush", "#FFFFFF");
                SetBrush(resources, "PillActiveTextBrush", "#1F2328");
                SetBrush(resources, "PillInactiveTextBrush", "#57606A");

                // Shimmer & Modals
                SetBrush(resources, "SkeletonBaseBrush", "#E1E4E8");
                SetBrush(resources, "SkeletonShimmerBrush", "#F6F8FA");
                SetBrush(resources, "OverlayBrush", "#800F172A");
            }
            else // Dark
            {
                // Core Surfaces (Deep Charcoal Identity)
                SetBrush(resources, "BgDarkBrush", "#0E1116");
                SetBrush(resources, "DarkBgBrush", "#0E1116");
                SetBrush(resources, "SurfaceCanvasBrush", "#0E1116");
                SetBrush(resources, "SidebarBgBrush", "#0A0D11");
                SetBrush(resources, "SurfaceSidebarBrush", "#0A0D11");
                SetBrush(resources, "CardBgBrush", "#161B22");
                SetBrush(resources, "SurfaceCardBrush", "#161B22");
                SetBrush(resources, "CardBgHoverBrush", "#1C2128");
                SetBrush(resources, "SurfaceCardHoverBrush", "#1C2128");
                SetBrush(resources, "CardBgPressedBrush", "#14181E");
                SetBrush(resources, "SurfaceElevatedBrush", "#21262D");
                SetBrush(resources, "SurfaceSunkenBrush", "#0A0C10");

                // Borders
                SetBrush(resources, "BorderBrush", "#30363D");
                SetBrush(resources, "BorderSubtleBrush", "#21262D");
                SetBrush(resources, "BorderStrongBrush", "#3D444D");
                SetBrush(resources, "BorderFocusBrush", "#388BFD");
                SetBrush(resources, "FocusRingBrush", "#388BFD");

                // Typography / Text
                SetBrush(resources, "TextPrimaryBrush", "#F0F3F6");
                SetBrush(resources, "TextSecondaryBrush", "#8B949E");
                SetBrush(resources, "TextMutedBrush", "#6E7681");
                SetBrush(resources, "TextDisabledBrush", "#484F58");
                SetBrush(resources, "TextOnAccentBrush", "#FFFFFF");

                // Brand / Accent
                SetBrush(resources, "AccentBrush", "#2F81F7");
                SetBrush(resources, "AccentHoverBrush", "#58A6FF");
                SetBrush(resources, "AccentPressedBrush", "#1F6FEB");
                SetBrush(resources, "AccentSubtleBrush", "#162438");

                // Semantics
                SetBrush(resources, "SuccessBrush", "#2EA043");
                SetBrush(resources, "SuccessSubtleBrush", "#0D2818");
                SetBrush(resources, "WarningBrush", "#D29922");
                SetBrush(resources, "WarningSubtleBrush", "#2E2005");
                SetBrush(resources, "DangerBrush", "#F85149");
                SetBrush(resources, "DangerSubtleBrush", "#341212");
                SetBrush(resources, "InfoBrush", "#388BFD");
                SetBrush(resources, "InfoSubtleBrush", "#0C2D6B");

                // Navigation
                SetBrush(resources, "NavActiveBgBrush", "#1F242C");
                SetBrush(resources, "NavActiveTextBrush", "#58A6FF");
                SetBrush(resources, "NavHoverBgBrush", "#161A20");
                SetBrush(resources, "HeaderBgBrush", "#0E1116");
                SetBrush(resources, "NavGroupTextBrush", "#6E7681");
                SetBrush(resources, "NavGroupBgBrush", "#0E1116");
                SetBrush(resources, "ArrowBrush", "#6E7681");
                SetBrush(resources, "ArrowActiveBrush", "#58A6FF");

                // Segmented Switch
                SetBrush(resources, "PillBgBrush", "#161B22");
                SetBrush(resources, "PillActiveBgBrush", "#2F81F7");
                SetBrush(resources, "PillActiveTextBrush", "#FFFFFF");
                SetBrush(resources, "PillInactiveTextBrush", "#8B949E");

                // Shimmer & Modals
                SetBrush(resources, "SkeletonBaseBrush", "#161B22");
                SetBrush(resources, "SkeletonShimmerBrush", "#21262D");
                SetBrush(resources, "OverlayBrush", "#CC080B0F");
            }

            ThemeChanged?.Invoke(theme);
        }

        private static void SetBrush(ResourceDictionary dict, string key, string hexColor)
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            dict[key] = new SolidColorBrush(color);
        }
    }
}
