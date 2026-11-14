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
                // Neutralized Minimalist macOS Sonoma Sidebar (#FAFAFA -> #F5F5F7 -> #F0F0F2)
                SetLinearGradient(resources, "SidebarGradientBrush", 
                    (Color)ColorConverter.ConvertFromString("#FAFAFA"),
                    (Color)ColorConverter.ConvertFromString("#F0F0F2"),
                    (Color)ColorConverter.ConvertFromString("#F5F5F7"));

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
                SetBrush(resources, "SidebarBgBrush", "#F5F5F7");
                SetBrush(resources, "SurfaceSidebarBrush", "#F5F5F7");
                SetBrush(resources, "SurfaceCardBrush", "#FFFFFF");
                SetBrush(resources, "SurfaceCardHoverBrush", "#FAFAFC");
                SetBrush(resources, "SurfaceElevatedBrush", "#FFFFFF");
                SetBrush(resources, "SurfaceSunkenBrush", "#F2F2F7");
                SetBrush(resources, "HeaderBgBrush", "#FFFFFF");

                // Typography / Text (macOS Sonoma Light)
                SetBrush(resources, "TextPrimaryBrush", "#1D1D1F");
                SetBrush(resources, "TextSecondaryBrush", "#6E6E73");
                SetBrush(resources, "TextMutedBrush", "#86868B");
                SetBrush(resources, "TextDisabledBrush", "#C7C7CC");
                SetBrush(resources, "TextOnAccentBrush", "#FFFFFF");

                // Unified Brand Blue (#0071E3) & Feedback
                SetBrush(resources, "AccentBrush", "#0071E3");
                SetBrush(resources, "AccentHoverBrush", "#0060C0");
                SetBrush(resources, "AccentPressedBrush", "#004FB3");
                SetBrush(resources, "AccentSubtleBrush", "#E8F1FD");

                SetBrush(resources, "SuccessBrush", "#34C759");
                SetBrush(resources, "SuccessSubtleBrush", "#E8F9ED");
                SetBrush(resources, "SuccessTextBrush", "#1B8C3D");
                SetBrush(resources, "WarningBrush", "#FF9500");
                SetBrush(resources, "WarningSubtleBrush", "#FFF8E6");
                SetBrush(resources, "WarningTextBrush", "#B26A00");
                SetBrush(resources, "DangerBrush", "#FF3B30");
                SetBrush(resources, "DangerSubtleBrush", "#FDECEC");
                SetBrush(resources, "DangerTextBrush", "#D70015");
                SetBrush(resources, "InfoBrush", "#0071E3");
                SetBrush(resources, "InfoSubtleBrush", "#E8F1FD");

                // Navigation & Pills (Soft Blue Selection #E8F1FD, Active Text #0071E3)
                SetBrush(resources, "NavActiveBgBrush", "#E8F1FD");
                SetBrush(resources, "NavActiveTextBrush", "#0071E3");
                SetBrush(resources, "NavHoverBgBrush", "#EBEBED");
                SetBrush(resources, "NavGroupTextBrush", "#6E6E73");
                SetBrush(resources, "NavGroupBgBrush", "#00000000");
                SetBrush(resources, "PillBgBrush", "#EBEBED");
                SetBrush(resources, "PillActiveBgBrush", "#FFFFFF");
                SetBrush(resources, "PillActiveTextBrush", "#1D1D1F");
                SetBrush(resources, "PillInactiveTextBrush", "#6E6E73");

                // macOS Specific
                SetBrush(resources, "MacOSSearchBgBrush", "#FFFFFF", 0.80);
                SetBrush(resources, "MacOSNavHoverBrush", "#0D000000");
                SetBrush(resources, "MacOSNavSelectedBrush", "#E8F1FD");
                SetBrush(resources, "FocusRingBrush", "#0071E3");
                SetBrush(resources, "OverlayBrush", "#30000000");
                SetBrush(resources, "SkeletonBaseBrush", "#EBEBED");
                SetBrush(resources, "SkeletonShimmerBrush", "#F5F5F7");

                // ScrollBars
                SetBrush(resources, "ScrollBarThumbBrush", "#D1D1D6");
                SetBrush(resources, "ScrollBarThumbHoverBrush", "#8E8E93");
                SetBrush(resources, "ScrollBarThumbPressedBrush", "#636366");
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

                // ScrollBars
                SetBrush(resources, "ScrollBarThumbBrush", "#48484A");
                SetBrush(resources, "ScrollBarThumbHoverBrush", "#636366");
                SetBrush(resources, "ScrollBarThumbPressedBrush", "#8E8E93");
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
