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

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RefreshCommand { get; }

        public HardwareInspectorViewModel(AdapterService adapterService)
        {
            _adapterService = adapterService;
            RefreshCommand = new RelayCommand(async () => await LoadAdaptersAsync());
        }

        public async Task LoadAdaptersAsync()
        {
            IsLoading = true;
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
            }
        }
    }
}

