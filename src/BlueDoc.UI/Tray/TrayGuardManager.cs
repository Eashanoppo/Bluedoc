using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Hardware;
using BlueDoc.Infrastructure.Services;

namespace BlueDoc.UI.Tray
{
    public class TrayGuardManager : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private System.Threading.Timer _monitorTimer;
        private readonly SetupApiHardwareInspector _hwInspector;
        private readonly WindowsServiceInspector _svcInspector;
        private AdapterStatus _lastKnownStatus = AdapterStatus.Operational;
        private bool _lastKnownServiceRunning = true;
        private Window _mainWindow;

        public bool IsMonitoringEnabled { get; private set; }

        public event EventHandler<string> HealthAlertTriggered;

        public TrayGuardManager()
        {
            _hwInspector = new SetupApiHardwareInspector();
            _svcInspector = new WindowsServiceInspector();
        }

        public void Initialize(Window mainWindow)
        {
            _mainWindow = mainWindow;

            _notifyIcon = new NotifyIcon
            {
                Text = "BlueDoc - Windows Bluetooth Doctor (Active Guard)",
                Icon = SystemIcons.Shield,
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open BlueDoc", null, (s, e) => ShowMainWindow());
            contextMenu.Items.Add("Run Background Health Probe", null, (s, e) => CheckHealth(triggerNotification: true));
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit BlueDoc", null, (s, e) =>
            {
                Dispose();
                System.Windows.Application.Current.Shutdown();
            });

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

            StartMonitoring();
        }

        public void StartMonitoring()
        {
            if (IsMonitoringEnabled) return;
            IsMonitoringEnabled = true;

            // Poll every 10 seconds (10,000 ms)
            _monitorTimer = new System.Threading.Timer(OnTimerTick, null, 5000, 10000);
        }

        public void StopMonitoring()
        {
            IsMonitoringEnabled = false;
            _monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void OnTimerTick(object state)
        {
            CheckHealth(triggerNotification: false);
        }

        public void CheckHealth(bool triggerNotification = false)
        {
            try
            {
                var adapters = _hwInspector.EnumerateAdapters();
                var primary = adapters.FirstOrDefault(a => a.IsPresent) ?? adapters.FirstOrDefault();
                var bthserv = _svcInspector.GetServiceInfo("bthserv");

                var currentStatus = primary?.Status ?? AdapterStatus.NotPresent;
                bool isRunning = bthserv != null && bthserv.IsRunning;

                // Check for adapter status degradation
                if (currentStatus != _lastKnownStatus)
                {
                    if (currentStatus == AdapterStatus.DeviceError)
                    {
                        string msg = $"CRITICAL ALERT: Bluetooth controller failed (Problem Code: {primary?.ProblemCode}). Controller may require power cycling or driver rollback.";
                        ShowAlert("BlueDoc Hardware Alert", msg, ToolTipIcon.Error);
                        HealthAlertTriggered?.Invoke(this, msg);
                    }
                    else if (currentStatus == AdapterStatus.Disabled)
                    {
                        string msg = "WARNING: Bluetooth controller has been administratively disabled in Windows.";
                        ShowAlert("BlueDoc Status Notice", msg, ToolTipIcon.Warning);
                        HealthAlertTriggered?.Invoke(this, msg);
                    }
                    else if (currentStatus == AdapterStatus.NotPresent)
                    {
                        string msg = "WARNING: Bluetooth adapter was abruptly disconnected or dropped from PnP bus.";
                        ShowAlert("BlueDoc Disconnection Alert", msg, ToolTipIcon.Warning);
                        HealthAlertTriggered?.Invoke(this, msg);
                    }
                    else if (currentStatus == AdapterStatus.Operational && _lastKnownStatus != AdapterStatus.Operational)
                    {
                        string msg = $"RESOLVED: Bluetooth controller '{primary?.FriendlyName}' is restored and operational.";
                        ShowAlert("BlueDoc Health Restored", msg, ToolTipIcon.Info);
                    }

                    _lastKnownStatus = currentStatus;
                }

                // Check for service status degradation
                if (isRunning != _lastKnownServiceRunning)
                {
                    if (!isRunning)
                    {
                        string msg = "WARNING: Windows Bluetooth Support Service (bthserv) has stopped. Nearby device discovery is paused.";
                        ShowAlert("BlueDoc Service Alert", msg, ToolTipIcon.Warning);
                        HealthAlertTriggered?.Invoke(this, msg);
                    }
                    else
                    {
                        string msg = "RESOLVED: Bluetooth Support Service is running.";
                        ShowAlert("BlueDoc Service Restored", msg, ToolTipIcon.Info);
                    }

                    _lastKnownServiceRunning = isRunning;
                }

                if (triggerNotification)
                {
                    string summary = $"Adapter: {currentStatus} | bthserv: {(isRunning ? "Running" : "Stopped")}";
                    ShowAlert("BlueDoc System Health", summary, ToolTipIcon.Info);
                }
            }
            catch { }
        }

        private void ShowAlert(string title, string message, ToolTipIcon icon)
        {
            if (_notifyIcon != null && _notifyIcon.Visible)
            {
                _notifyIcon.ShowBalloonTip(4000, title, message, icon);
            }
        }

        private void ShowMainWindow()
        {
            if (_mainWindow != null)
            {
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.Show();
                    _mainWindow.WindowState = WindowState.Normal;
                    _mainWindow.Activate();
                });
            }
        }

        public void Dispose()
        {
            StopMonitoring();
            if (_monitorTimer != null)
            {
                _monitorTimer.Dispose();
                _monitorTimer = null;
            }
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }
}
