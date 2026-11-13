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
                SetBrush(resources, "BgDarkBrush", "#F8FAFC");
                SetBrush(resources, "DarkBgBrush", "#F8FAFC");
                SetBrush(resources, "SidebarBgBrush", "#FFFFFF");
                SetBrush(resources, "CardBgBrush", "#FFFFFF");
                SetBrush(resources, "CardBgHoverBrush", "#F1F5F9");
                SetBrush(resources, "BorderBrush", "#E2E8F0");
                SetBrush(resources, "TextPrimaryBrush", "#0F172A");
                SetBrush(resources, "TextSecondaryBrush", "#475569");
                SetBrush(resources, "TextMutedBrush", "#94A3B8");
                SetBrush(resources, "AccentBrush", "#2563EB");
                SetBrush(resources, "AccentHoverBrush", "#1D4ED8");
                SetBrush(resources, "NavActiveBgBrush", "#EFF6FF");
                SetBrush(resources, "NavActiveTextBrush", "#2563EB");
                SetBrush(resources, "NavHoverBgBrush", "#F1F5F9");
                SetBrush(resources, "HeaderBgBrush", "#FFFFFF");
                SetBrush(resources, "NavGroupTextBrush", "#64748B");
                SetBrush(resources, "NavGroupBgBrush", "#F8FAFC");
                SetBrush(resources, "ArrowBrush", "#94A3B8");
                SetBrush(resources, "ArrowActiveBrush", "#2563EB");
                SetBrush(resources, "PillBgBrush", "#E2E8F0");
                SetBrush(resources, "PillActiveBgBrush", "#FFFFFF");
                SetBrush(resources, "PillActiveTextBrush", "#0F172A");
                SetBrush(resources, "PillInactiveTextBrush", "#64748B");
            }
            else // Dark
            {
                SetBrush(resources, "BgDarkBrush", "#0A0E17");
                SetBrush(resources, "DarkBgBrush", "#0A0E17");
                SetBrush(resources, "SidebarBgBrush", "#101726");
                SetBrush(resources, "CardBgBrush", "#161F30");
                SetBrush(resources, "CardBgHoverBrush", "#1C273C");
                SetBrush(resources, "BorderBrush", "#243247");
                SetBrush(resources, "TextPrimaryBrush", "#F1F5F9");
                SetBrush(resources, "TextSecondaryBrush", "#94A3B8");
                SetBrush(resources, "TextMutedBrush", "#64748B");
                SetBrush(resources, "AccentBrush", "#3B82F6");
                SetBrush(resources, "AccentHoverBrush", "#60A5FA");
                SetBrush(resources, "NavActiveBgBrush", "#1E2A40");
                SetBrush(resources, "NavActiveTextBrush", "#60A5FA");
                SetBrush(resources, "NavHoverBgBrush", "#1A2438");
                SetBrush(resources, "HeaderBgBrush", "#101726");
                SetBrush(resources, "NavGroupTextBrush", "#64748B");
                SetBrush(resources, "NavGroupBgBrush", "#131C2D");
                SetBrush(resources, "ArrowBrush", "#475569");
                SetBrush(resources, "ArrowActiveBrush", "#60A5FA");
                SetBrush(resources, "PillBgBrush", "#1C273C");
                SetBrush(resources, "PillActiveBgBrush", "#2563EB");
                SetBrush(resources, "PillActiveTextBrush", "#FFFFFF");
                SetBrush(resources, "PillInactiveTextBrush", "#94A3B8");
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
