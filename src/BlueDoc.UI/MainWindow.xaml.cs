using System.Windows;
using BlueDoc.UI.Tray;
using BlueDoc.UI.ViewModels;

namespace BlueDoc.UI
{
    public partial class MainWindow : Window
    {
        private TrayGuardManager _trayGuard;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                await vm.InitializeAsync();
            }

            // Initialize System Tray Guard
            try
            {
                _trayGuard = new TrayGuardManager();
                _trayGuard.Initialize(this);
            }
            catch
            {
                // Non-fatal if system tray unavailable
            }
        }

        private void MainWindow_Closed(object sender, System.EventArgs e)
        {
            _trayGuard?.Dispose();
            _trayGuard = null;
        }
    }
}
