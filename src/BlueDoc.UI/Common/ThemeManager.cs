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
        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

        public static event Action<AppTheme> ThemeChanged;

        public static void SetTheme(AppTheme theme)
        {
            CurrentTheme = theme;
            var resources = System.Windows.Application.Current?.Resources;
            if (resources == null) return;

            if (theme == AppTheme.Light)
            {
                // macOS Sonoma Sidebar Gradient (#F7D6C4 -> #E8D5F5 -> #D4C5F0)
                SetLinearGradient(resources, "SidebarGradientBrush", 
                    (Color)ColorConverter.ConvertFromString("#F7D6C4"),
                    (Color)ColorConverter.ConvertFromString("#D4C5F0"),
                    (Color)ColorConverter.ConvertFromString("#E8D5F5"));

                // Core Canvas & Surfaces
                SetBrush(resources, "MainBgBrush", "#FFFFFF");
                SetBrush(resources, "CardBgBrush", "#FFFFFF");
                SetBrush(resources, "CardBorderBrush", "#E5E5E7");
                SetBrush(resources, "BorderBrush", "#E5E5E7");
                SetBrush(resources, "BorderSubtleBrush", "#ECECEE");
                SetBrush(resources, "BorderStrongBrush", "#D1D1D6");

                SetBrush(resources, "BgDarkBrush", "#FFFFFF");
                SetBrush(resources, "DarkBgBrush", "#FFFFFF");
                SetBrush(resources, "SurfaceCanvasBrush", "#FFFFFF");
                SetBrush(resources, "SidebarBgBrush", "#F5F2F9");
                SetBrush(resources, "SurfaceSidebarBrush", "#F5F2F9");
                SetBrush(resources, "SurfaceCardBrush", "#FFFFFF");
                SetBrush(resources, "SurfaceCardHoverBrush", "#F9F9FB");
                SetBrush(resources, "SurfaceElevatedBrush", "#FFFFFF");
                SetBrush(resources, "SurfaceSunkenBrush", "#F2F2F7");
                SetBrush(resources, "HeaderBgBrush", "#FFFFFF");

                // Typography / Text (macOS Sonoma Light)
                SetBrush(resources, "TextPrimaryBrush", "#1D1D1F");
                SetBrush(resources, "TextSecondaryBrush", "#6E6E73");
                SetBrush(resources, "TextMutedBrush", "#86868B");
                SetBrush(resources, "TextDisabledBrush", "#C7C7CC");
                SetBrush(resources, "TextOnAccentBrush", "#FFFFFF");

                // Brand / Accent (#007AFF) & Feedback
                SetBrush(resources, "AccentBrush", "#007AFF");
                SetBrush(resources, "AccentHoverBrush", "#0062CC");
                SetBrush(resources, "AccentPressedBrush", "#004FB3");
                SetBrush(resources, "AccentSubtleBrush", "#E5F1FF");

                SetBrush(resources, "SuccessBrush", "#34C759");
                SetBrush(resources, "SuccessSubtleBrush", "#E8F8ED");
                SetBrush(resources, "WarningBrush", "#FF9500");
                SetBrush(resources, "WarningSubtleBrush", "#FFF4E5");
                SetBrush(resources, "DangerBrush", "#FF3B30");
                SetBrush(resources, "DangerSubtleBrush", "#FDECEC");
                SetBrush(resources, "InfoBrush", "#007AFF");
                SetBrush(resources, "InfoSubtleBrush", "#E5F1FF");

                // Navigation & Pills
                SetBrush(resources, "NavActiveBgBrush", "#007AFF");
                SetBrush(resources, "NavActiveTextBrush", "#FFFFFF");
                SetBrush(resources, "NavHoverBgBrush", "#E5E5EA");
                SetBrush(resources, "NavGroupTextBrush", "#6E6E73");
                SetBrush(resources, "NavGroupBgBrush", "#00000000");
                SetBrush(resources, "PillBgBrush", "#E5E5EA");
                SetBrush(resources, "PillActiveBgBrush", "#FFFFFF");
                SetBrush(resources, "PillActiveTextBrush", "#1D1D1F");
                SetBrush(resources, "PillInactiveTextBrush", "#6E6E73");

                // macOS Specific
                SetBrush(resources, "MacOSSearchBgBrush", "#FFFFFF", 0.45);
                SetBrush(resources, "MacOSNavHoverBrush", "#FFFFFF", 0.15);
                SetBrush(resources, "MacOSNavSelectedBrush", "#FFFFFF", 0.25);
                SetBrush(resources, "FocusRingBrush", "#007AFF");
                SetBrush(resources, "OverlayBrush", "#40000000");
                SetBrush(resources, "SkeletonBaseBrush", "#E5E5EA");
                SetBrush(resources, "SkeletonShimmerBrush", "#F2F2F7");
            }
            else // Dark
            {
                // macOS Sonoma Sidebar Gradient (#2C2C2E -> #1C1C1E)
                SetLinearGradient(resources, "SidebarGradientBrush", 
                    (Color)ColorConverter.ConvertFromString("#2C2C2E"),
                    (Color)ColorConverter.ConvertFromString("#1C1C1E"));

                // Core Canvas & Surfaces
                SetBrush(resources, "MainBgBrush", "#1E1E1E");
                SetBrush(resources, "CardBgBrush", "#2C2C2E");
                SetBrush(resources, "CardBorderBrush", "#3A3A3C");
                SetBrush(resources, "BorderBrush", "#3A3A3C");
                SetBrush(resources, "BorderSubtleBrush", "#323234");
                SetBrush(resources, "BorderStrongBrush", "#48484A");

                SetBrush(resources, "BgDarkBrush", "#1E1E1E");
                SetBrush(resources, "DarkBgBrush", "#1E1E1E");
                SetBrush(resources, "SurfaceCanvasBrush", "#1E1E1E");
                SetBrush(resources, "SidebarBgBrush", "#2C2C2E");
                SetBrush(resources, "SurfaceSidebarBrush", "#2C2C2E");
                SetBrush(resources, "SurfaceCardBrush", "#2C2C2E");
                SetBrush(resources, "SurfaceCardHoverBrush", "#353538");
                SetBrush(resources, "SurfaceElevatedBrush", "#323235");
                SetBrush(resources, "SurfaceSunkenBrush", "#161618");
                SetBrush(resources, "HeaderBgBrush", "#1E1E1E");

                // Typography / Text (macOS Sonoma Dark)
                SetBrush(resources, "TextPrimaryBrush", "#F5F5F7");
                SetBrush(resources, "TextSecondaryBrush", "#86868B");
                SetBrush(resources, "TextMutedBrush", "#636366");
                SetBrush(resources, "TextDisabledBrush", "#48484A");
                SetBrush(resources, "TextOnAccentBrush", "#FFFFFF");

                // Brand / Accent (#0A84FF) & Feedback
                SetBrush(resources, "AccentBrush", "#0A84FF");
                SetBrush(resources, "AccentHoverBrush", "#409CFF");
                SetBrush(resources, "AccentPressedBrush", "#0066D6");
                SetBrush(resources, "AccentSubtleBrush", "#19324D");

                SetBrush(resources, "SuccessBrush", "#34C759");
                SetBrush(resources, "SuccessSubtleBrush", "#13331D");
                SetBrush(resources, "WarningBrush", "#FF9F0A");
                SetBrush(resources, "WarningSubtleBrush", "#332205");
                SetBrush(resources, "DangerBrush", "#FF453A");
                SetBrush(resources, "DangerSubtleBrush", "#361514");
                SetBrush(resources, "InfoBrush", "#0A84FF");
                SetBrush(resources, "InfoSubtleBrush", "#19324D");

                // Navigation & Pills
                SetBrush(resources, "NavActiveBgBrush", "#0A84FF");
                SetBrush(resources, "NavActiveTextBrush", "#FFFFFF");
                SetBrush(resources, "NavHoverBgBrush", "#323236");
                SetBrush(resources, "NavGroupTextBrush", "#86868B");
                SetBrush(resources, "NavGroupBgBrush", "#00000000");
                SetBrush(resources, "PillBgBrush", "#323236");
                SetBrush(resources, "PillActiveBgBrush", "#48484A");
                SetBrush(resources, "PillActiveTextBrush", "#F5F5F7");
                SetBrush(resources, "PillInactiveTextBrush", "#86868B");

                // macOS Specific
                SetBrush(resources, "MacOSSearchBgBrush", "#000000", 0.30);
                SetBrush(resources, "MacOSNavHoverBrush", "#FFFFFF", 0.10);
                SetBrush(resources, "MacOSNavSelectedBrush", "#FFFFFF", 0.20);
                SetBrush(resources, "FocusRingBrush", "#0A84FF");
                SetBrush(resources, "OverlayBrush", "#80000000");
                SetBrush(resources, "SkeletonBaseBrush", "#2C2C2E");
                SetBrush(resources, "SkeletonShimmerBrush", "#3A3A3C");
            }

            ThemeChanged?.Invoke(theme);
        }

        private static void SetBrush(ResourceDictionary dict, string key, string hexColor, double opacity = 1.0)
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            dict[key] = new SolidColorBrush(color) { Opacity = opacity };
        }

        private static void SetLinearGradient(ResourceDictionary dict, string key, Color c1, Color c2, Color? cMid = null)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };

            if (cMid.HasValue)
            {
                brush.GradientStops.Add(new GradientStop(c1, 0.0));
                brush.GradientStops.Add(new GradientStop(cMid.Value, 0.5));
                brush.GradientStops.Add(new GradientStop(c2, 1.0));
            }
            else
            {
                brush.GradientStops.Add(new GradientStop(c1, 0.0));
                brush.GradientStops.Add(new GradientStop(c2, 1.0));
            }

            dict[key] = brush;
        }
    }
}
