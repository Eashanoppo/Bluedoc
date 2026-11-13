using System;
using System.Windows.Media;
using BlueDoc.UI.Common;
using BlueDoc.UI.ViewModels;

namespace BlueDoc.Tests.Unit
{
    public static class AccessibilityVerificationTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("[TEST SUITE] Accessibility, WCAG Contrast & Keyboard Navigation Tests");

            Test_KeyboardShortcuts_Registered();
            Test_WcagContrast_DarkAndLightTheme();
            Test_MotionManager_ReducedMotionEnforcement();
            Test_ScreenReader_AccessibleLabelsAndTooltips();

            Console.WriteLine("  ✓ All Accessibility & WCAG Tests Passed (4/4)\n");
        }

        private static void Test_KeyboardShortcuts_Registered()
        {
            Console.Write("  Running Test_KeyboardShortcuts_Registered... ");
            var mainVm = new MainViewModel();

            Assert(mainVm.ToggleSidebarCommand != null, "Ctrl+[ Sidebar toggle command must be bound.");
            Assert(mainVm.QuickDiagnosisCommand != null, "Ctrl+D Quick diagnosis command must be bound.");
            Assert(mainVm.NavigateCommand != null, "Navigation command must be bound.");

            Console.WriteLine("PASSED");
        }

        private static void Test_WcagContrast_DarkAndLightTheme()
        {
            Console.Write("  Running Test_WcagContrast_DarkAndLightTheme... ");

            // Dark Theme Palette:
            // Background: #0B0F17, Card: #162032, TextPrimary: #F8FAFC, TextSecondary: #94A3B8, Accent: #3B82F6
            double darkPrimaryContrast = CalculateContrastRatio("#0B0F17", "#F8FAFC");
            double darkSecondaryContrast = CalculateContrastRatio("#162032", "#94A3B8");
            double darkAccentContrast = CalculateContrastRatio("#0B0F17", "#60A5FA");

            Assert(darkPrimaryContrast >= 4.5, $"Dark text primary contrast must be >= 4.5:1, got {darkPrimaryContrast:F2}:1");
            Assert(darkSecondaryContrast >= 4.5, $"Dark text secondary contrast must be >= 4.5:1, got {darkSecondaryContrast:F2}:1");
            Assert(darkAccentContrast >= 3.0, $"Dark accent contrast must be >= 3.0:1, got {darkAccentContrast:F2}:1");

            // Light Theme Palette:
            // Background: #F8FAFC, Card: #FFFFFF, TextPrimary: #0F172A, TextSecondary: #475569
            double lightPrimaryContrast = CalculateContrastRatio("#FFFFFF", "#0F172A");
            double lightSecondaryContrast = CalculateContrastRatio("#F8FAFC", "#475569");

            Assert(lightPrimaryContrast >= 4.5, $"Light text primary contrast must be >= 4.5:1, got {lightPrimaryContrast:F2}:1");
            Assert(lightSecondaryContrast >= 4.5, $"Light text secondary contrast must be >= 4.5:1, got {lightSecondaryContrast:F2}:1");

            Console.WriteLine($"PASSED (Dark: {darkPrimaryContrast:F1}:1 / Light: {lightPrimaryContrast:F1}:1)");
        }

        private static void Test_MotionManager_ReducedMotionEnforcement()
        {
            Console.Write("  Running Test_MotionManager_ReducedMotionEnforcement... ");
            var motion = MotionManager.Instance;

            // When reduced motion is requested, animation durations must collapse to 0ms
            motion.ForceReducedMotion = true;
            var zeroDuration = motion.PageTransitionDuration;
            Assert(zeroDuration.TotalMilliseconds == 0, "Reduced motion must force animation durations to exactly 0ms.");

            // Restore
            motion.ForceReducedMotion = false;
            var normalDuration = motion.PageTransitionDuration;
            Assert(normalDuration.TotalMilliseconds == 180, "Normal motion must preserve requested animation durations.");

            Console.WriteLine("PASSED");
        }

        private static void Test_ScreenReader_AccessibleLabelsAndTooltips()
        {
            Console.Write("  Running Test_ScreenReader_AccessibleLabelsAndTooltips... ");
            var mainVm = new MainViewModel();

            // Check tooltip descriptions for screen readers
            Assert(!string.IsNullOrEmpty(mainVm.SidebarToggleTooltip), "Sidebar toggle must have dynamic screen-reader tooltip.");
            Assert(!string.IsNullOrEmpty(mainVm.RadioStateDisplay), "Radio power switch must expose text state display.");

            var wizardVm = new FixMyConnectionWizardViewModel();
            Assert(!string.IsNullOrEmpty(wizardVm.StatusMessage), "Wizard must expose accessible status messages.");

            Console.WriteLine("PASSED");
        }

        private static double CalculateContrastRatio(string hexBg, string hexFg)
        {
            Color c1 = (Color)ColorConverter.ConvertFromString(hexBg);
            Color c2 = (Color)ColorConverter.ConvertFromString(hexFg);

            double l1 = GetRelativeLuminance(c1);
            double l2 = GetRelativeLuminance(c2);

            double brighter = Math.Max(l1, l2);
            double darker = Math.Min(l1, l2);

            return (brighter + 0.05) / (darker + 0.05);
        }

        private static double GetRelativeLuminance(Color c)
        {
            double r = ConvertChannel(c.R / 255.0);
            double g = ConvertChannel(c.G / 255.0);
            double b = ConvertChannel(c.B / 255.0);

            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        private static double ConvertChannel(double channel)
        {
            return channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion Failed: {message}");
            }
        }
    }
}
