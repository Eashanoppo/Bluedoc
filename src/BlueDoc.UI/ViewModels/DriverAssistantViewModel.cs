using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Core.Models;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class DriverAssistantViewModel : ViewModelBase
    {
        private readonly AdapterService _adapterService;
        private readonly DriverService _driverService;

        private BluetoothAdapterInfo _adapter;
        private string _driverStatusExplanation = string.Empty;
        private string _rollbackGuide = string.Empty;
        private string _reinstallGuide = string.Empty;
        private string _supportUrl = string.Empty;
        private bool _isLoading;

        public BluetoothAdapterInfo Adapter
        {
            get => _adapter;
            set => SetProperty(ref _adapter, value);
        }

        public string DriverStatusExplanation
        {
            get => _driverStatusExplanation;
            set => SetProperty(ref _driverStatusExplanation, value);
        }

        public string RollbackGuide
        {
            get => _rollbackGuide;
            set => SetProperty(ref _rollbackGuide, value);
        }

        public string ReinstallGuide
        {
            get => _reinstallGuide;
            set => SetProperty(ref _reinstallGuide, value);
        }

        public string SupportUrl
        {
            get => _supportUrl;
            set => SetProperty(ref _supportUrl, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand OpenDeviceManagerCommand { get; }
        public ICommand OpenSupportPageCommand { get; }
        public ICommand OpenSettingsCommand { get; }

        public ICommand OpenIntelPortalCommand { get; }
        public ICommand OpenRealtekPortalCommand { get; }
        public ICommand OpenBroadcomPortalCommand { get; }
        public ICommand OpenQualcommPortalCommand { get; }
        public ICommand OpenMicrosoftPortalCommand { get; }

        public DriverAssistantViewModel(AdapterService adapterService = null, DriverService driverService = null)
        {
            _adapterService = adapterService ?? new AdapterService();
            _driverService = driverService ?? new DriverService();

            RefreshCommand = new RelayCommand(async () => await LoadDriverInfoAsync());
            OpenDeviceManagerCommand = new RelayCommand(() => _driverService.OpenDeviceManager());
            OpenSupportPageCommand = new RelayCommand(() => OpenUrl(SupportUrl));
            OpenSettingsCommand = new RelayCommand(() => _driverService.OpenWindowsBluetoothSettings());

            OpenIntelPortalCommand = new RelayCommand(() => OpenUrl("https://www.intel.com/content/www/us/en/support/articles/000005489/wireless.html"));
            OpenRealtekPortalCommand = new RelayCommand(() => OpenUrl("https://www.realtek.com/en/component/zoo/category/pci-wireless-lan-pci-express-software"));
            OpenBroadcomPortalCommand = new RelayCommand(() => OpenUrl("https://www.broadcom.com/support"));
            OpenQualcommPortalCommand = new RelayCommand(() => OpenUrl("https://www.qualcomm.com/products/features/bluetooth"));
            OpenMicrosoftPortalCommand = new RelayCommand(() => OpenUrl("https://support.microsoft.com/en-us/windows/fix-bluetooth-problems-in-windows-723e05f8-0376-46a4-73ac-eeb2d8442a66"));
        }

        private void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch { }
        }

        public async Task LoadDriverInfoAsync()
        {
            IsLoading = true;
            try
            {
                Adapter = await _adapterService.GetPrimaryAdapterAsync();

                if (Adapter != null)
                {
                    DriverStatusExplanation = _driverService.ExplainProblemCode((int)Adapter.ProblemCode);
                    SupportUrl = _driverService.GetManufacturerSupportLink(Adapter.HardwareDetails);
                    RollbackGuide = _driverService.GetRollbackGuide(Adapter.FriendlyName);
                    ReinstallGuide = _driverService.GetReinstallationGuide(Adapter.FriendlyName, SupportUrl);
                }
                else
                {
                    DriverStatusExplanation = "No Bluetooth adapter detected in the system. Check if the adapter is disconnected or disabled.";
                    SupportUrl = "https://support.microsoft.com/en-us/windows/fix-bluetooth-problems-in-windows-723e05f8-0376-46a4-73ac-eeb2d8442a66";
                    RollbackGuide = _driverService.GetRollbackGuide(null);
                    ReinstallGuide = _driverService.GetReinstallationGuide(null, SupportUrl);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
