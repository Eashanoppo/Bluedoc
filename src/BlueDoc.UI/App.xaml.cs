using System;
using System.Diagnostics;
using System.Windows;
using BlueDoc.UI.Cli;

namespace BlueDoc.UI
{
    public partial class App : System.Windows.Application
    {
        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Trace.WriteLine($"[CRITICAL UI EXCEPTION] {e.Exception.Message}\n{e.Exception.StackTrace}");
            try
            {
                System.IO.File.WriteAllText(@"E:\BlueDoc\crash_log.txt", $"{e.Exception}\n\nInnerException: {e.Exception.InnerException}");
            }
            catch { }
            MessageBox.Show($"An unexpected UI exception occurred:\n\n{e.Exception.Message}\n\nThe operation was safely handled.", "BlueDoc Protection", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Trace.WriteLine($"[FATAL DOMAIN EXCEPTION] {ex.Message}\n{ex.StackTrace}");
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (CliRunner.IsCliInvocation(e.Args))
            {
                int exitCode = await CliRunner.RunAsync(e.Args);
                Environment.Exit(exitCode);
                return;
            }

            // Legacy CPU & Integrated GPU Stability Fallback (e.g. Intel Gen 1 Core i3/i5 / GMA / Ironlake)
            try
            {
                int renderingTier = (System.Windows.Media.RenderCapability.Tier >> 16);
                if (renderingTier == 0)
                {
                    // Fall back to software rendering to avoid DirectX driver crashes on older Intel HD Graphics
                    System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"WPF Rendering Tier check exception: {ex.Message}");
            }

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
