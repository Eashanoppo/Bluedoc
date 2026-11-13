using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using BlueDoc.Application.Services;
using BlueDoc.Core.Models;
using BlueDoc.UI.Common;

namespace BlueDoc.UI.ViewModels
{
    public class HardwareInspectorViewModel : ViewModelBase
    {
        private readonly AdapterService _adapterService;
        private BluetoothAdapterInfo _selectedAdapter;
        private bool _isLoading;
        private bool _isRefreshing;
        private string _statusMessage = "Ready";

        public ObservableCollection<BluetoothAdapterInfo> Adapters { get; } = new ObservableCollection<BluetoothAdapterInfo>();

        public BluetoothAdapterInfo SelectedAdapter
        {
            get => _selectedAdapter;
            set => SetProperty(ref _selectedAdapter, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand CopyHardwareIdCommand { get; }
        public ICommand CopyInstanceIdCommand { get; }

        public HardwareInspectorViewModel(AdapterService adapterService)
        {
            _adapterService = adapterService;
            RefreshCommand = new RelayCommand(async () => await LoadAdaptersAsync());

            CopyHardwareIdCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrEmpty(SelectedAdapter?.HardwareId))
                {
                    try
                    {
                        System.Windows.Clipboard.SetText(SelectedAdapter.HardwareId);
                        NotificationService.Instance.ShowSuccess("Copied", "Hardware ID copied to clipboard.");
                    }
                    catch { }
                }
            }, () => !string.IsNullOrEmpty(SelectedAdapter?.HardwareId));

            CopyInstanceIdCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrEmpty(SelectedAdapter?.DeviceInstanceId))
                {
                    try
                    {
                        System.Windows.Clipboard.SetText(SelectedAdapter.DeviceInstanceId);
                        NotificationService.Instance.ShowSuccess("Copied", "Device Instance ID copied to clipboard.");
                    }
                    catch { }
                }
            }, () => !string.IsNullOrEmpty(SelectedAdapter?.DeviceInstanceId));
        }

        public async Task LoadAdaptersAsync()
        {
            if (Adapters.Count == 0)
            {
                IsLoading = true;
            }
            else
            {
                IsRefreshing = true;
            }

            StatusMessage = "Enumerating Plug and Play devices via SetupAPI...";

            try
            {
                var list = await _adapterService.GetAdaptersAsync();
                Adapters.Clear();
                foreach (var item in list)
                {
                    Adapters.Add(item);
                }

                if (Adapters.Count > 0)
                {
                    SelectedAdapter = Adapters[0];
                    StatusMessage = $"Detected {Adapters.Count} Bluetooth device node(s).";
                }
                else
                {
                    SelectedAdapter = null;
                    StatusMessage = "No Bluetooth adapters detected.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Hardware inspection error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }
    }
}

