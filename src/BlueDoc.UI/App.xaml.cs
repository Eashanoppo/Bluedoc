using System;
using System.Windows;
using BlueDoc.UI.Cli;

namespace BlueDoc.UI
{
    public partial class App : System.Windows.Application
    {
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
            catch { }

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
