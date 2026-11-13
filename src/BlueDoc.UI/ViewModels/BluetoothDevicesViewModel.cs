using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Core.Models;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class BluetoothDevicesViewModel : ViewModelBase
    {
        private readonly DeviceManagementService _managementService;
        private BluetoothDeviceInfo _selectedDevice;
        private bool _isLoading;
        private string _statusMessage = "Ready";

        public ObservableCollection<BluetoothDeviceInfo> PairedDevices { get; } = new ObservableCollection<BluetoothDeviceInfo>();

        public BluetoothDeviceInfo SelectedDevice
        {
            get => _selectedDevice;
            set => SetProperty(ref _selectedDevice, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand UnpairDeviceCommand { get; }
        public ICommand AddDeviceCommand { get; }
        public ICommand OpenDeviceManagerCommand { get; }

        public BluetoothDevicesViewModel(DeviceManagementService managementService)
        {
            _managementService = managementService;

            RefreshCommand = new RelayCommand(async () => await LoadPairedDevicesAsync());

            UnpairDeviceCommand = new RelayCommand(async () =>
            {
                if (SelectedDevice == null) return;

                var result = MessageBox.Show(
                    $"Are you sure you want to remove and unpair '{SelectedDevice.DisplayName}' ({SelectedDevice.BluetoothAddress}) from Windows?\n\nYou will need to put the device in pairing mode again to reconnect.",
                    "Unpair Bluetooth Device Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    IsLoading = true;
                    StatusMessage = $"Removing device '{SelectedDevice.DisplayName}'...";
                    bool success = await _managementService.UnpairDeviceAsync(SelectedDevice.BluetoothAddress);
                    if (success)
                    {
                        StatusMessage = $"Device '{SelectedDevice.DisplayName}' removed successfully.";
                        await LoadPairedDevicesAsync();
                    }
                    else
                    {
                        StatusMessage = $"Could not remove device '{SelectedDevice.DisplayName}'. It may be in use or require Windows elevation.";
                        MessageBox.Show(
                            "Windows could not remove this Bluetooth device. Opening Windows Bluetooth Settings where you can remove it directly.",
                            "Removal Notification",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        _managementService.LaunchWindowsPairingWizard();
                    }
                    IsLoading = false;
                }
            }, () => SelectedDevice != null && !IsLoading);

            AddDeviceCommand = new RelayCommand(() =>
            {
                _managementService.LaunchWindowsPairingWizard();
            });

            OpenDeviceManagerCommand = new RelayCommand(() =>
            {
                _managementService.LaunchDeviceManager();
            });
        }

        public async Task LoadPairedDevicesAsync()
        {
            IsLoading = true;
            StatusMessage = "Retrieving paired Bluetooth devices from Windows registry and PnP...";

            try
            {
                var list = await _managementService.GetPairedDevicesAsync();
                PairedDevices.Clear();
                foreach (var device in list)
                {
                    PairedDevices.Add(device);
                }

                if (PairedDevices.Count > 0)
                {
                    SelectedDevice = PairedDevices[0];
                    StatusMessage = $"Found {PairedDevices.Count} paired or remembered Bluetooth device(s).";
                }
                else
                {
                    SelectedDevice = null;
                    StatusMessage = "No paired Bluetooth devices detected in Windows.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error retrieving paired devices: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}

