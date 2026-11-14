using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using BlueDoc.Application.Services;
using BlueDoc.Core.Models;

namespace BlueDoc.UI.Views
{
    public partial class DeviceCard : UserControl
    {
        public static readonly DependencyProperty DeviceNameProperty =
            DependencyProperty.Register(
                nameof(DeviceName), 
                typeof(string), 
                typeof(DeviceCard), 
                new PropertyMetadata("Bluetooth Device"));

        public static readonly DependencyProperty DeviceTypeProperty =
            DependencyProperty.Register(
                nameof(DeviceType), 
                typeof(string), 
                typeof(DeviceCard), 
                new PropertyMetadata("Audio / Peripheral"));

        public static readonly DependencyProperty MacAddressProperty =
            DependencyProperty.Register(
                nameof(MacAddress), 
                typeof(string), 
                typeof(DeviceCard), 
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsConnectedProperty =
            DependencyProperty.Register(
                nameof(IsConnected), 
                typeof(bool), 
                typeof(DeviceCard), 
                new PropertyMetadata(false));

        public static readonly DependencyProperty ConnectCommandProperty =
            DependencyProperty.Register(
                nameof(ConnectCommand), 
                typeof(ICommand), 
                typeof(DeviceCard), 
                new PropertyMetadata(null));

        public static readonly DependencyProperty ManageCommandProperty =
            DependencyProperty.Register(
                nameof(ManageCommand), 
                typeof(ICommand), 
                typeof(DeviceCard), 
                new PropertyMetadata(null));

        private BluetoothDeviceInfo _trackedInfo;

        public string DeviceName
        {
            get => (string)GetValue(DeviceNameProperty);
            set => SetValue(DeviceNameProperty, value);
        }

        public string DeviceType
        {
            get => (string)GetValue(DeviceTypeProperty);
            set => SetValue(DeviceTypeProperty, value);
        }

        public string MacAddress
        {
            get => (string)GetValue(MacAddressProperty);
            set => SetValue(MacAddressProperty, value);
        }

        public bool IsConnected
        {
            get => (bool)GetValue(IsConnectedProperty);
            set => SetValue(IsConnectedProperty, value);
        }

        public ICommand ConnectCommand
        {
            get => (ICommand)GetValue(ConnectCommandProperty);
            set => SetValue(ConnectCommandProperty, value);
        }

        public ICommand ManageCommand
        {
            get => (ICommand)GetValue(ManageCommandProperty);
            set => SetValue(ManageCommandProperty, value);
        }

        public DeviceCard()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += (s, e) => DetachTrackedInfo();
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            var anim = new DoubleAnimation(-2, TimeSpan.FromSeconds(0.2))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            CardOffsetTransform?.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, anim);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            var anim = new DoubleAnimation(0, TimeSpan.FromSeconds(0.2))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            CardOffsetTransform?.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, anim);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DetachTrackedInfo();

            if (e.NewValue is BluetoothDeviceInfo info)
            {
                _trackedInfo = info;
                _trackedInfo.PropertyChanged += OnDeviceInfoPropertyChanged;
                UpdateFromDeviceInfo(info);
            }
        }

        private void DetachTrackedInfo()
        {
            if (_trackedInfo != null)
            {
                _trackedInfo.PropertyChanged -= OnDeviceInfoPropertyChanged;
                _trackedInfo = null;
            }
        }

        private void OnDeviceInfoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is BluetoothDeviceInfo info)
            {
                Dispatcher.Invoke(() => UpdateFromDeviceInfo(info));
            }
        }

        private void UpdateFromDeviceInfo(BluetoothDeviceInfo info)
        {
            DeviceName = info.DisplayName;
            DeviceType = info.DeviceTypeDisplay;
            MacAddress = info.BluetoothAddress;
            IsConnected = info.IsConnected;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var target = DataContext;
            if (ConnectCommand != null)
            {
                if (ConnectCommand.CanExecute(target))
                {
                    ConnectCommand.Execute(target);
                    return;
                }
                if (ConnectCommand.CanExecute(null))
                {
                    ConnectCommand.Execute(null);
                    return;
                }
            }

            // Direct fallback via DeviceManagementService
            if (target is BluetoothDeviceInfo device)
            {
                try
                {
                    var mgmt = new DeviceManagementService();
                    if (device.IsConnected)
                    {
                        var res = await mgmt.DisconnectDeviceAsync(device);
                        if (res.Success)
                        {
                            device.IsConnected = false;
                            IsConnected = false;
                        }
                    }
                    else
                    {
                        var res = await mgmt.ConnectDeviceAsync(device);
                        if (res.Success)
                        {
                            device.IsConnected = true;
                            IsConnected = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DeviceCard] Connection error: {ex.Message}");
                }
            }
        }

        private void ManageButton_Click(object sender, RoutedEventArgs e)
        {
            var target = DataContext;
            if (ManageCommand != null)
            {
                if (ManageCommand.CanExecute(target))
                {
                    ManageCommand.Execute(target);
                    return;
                }
                if (ManageCommand.CanExecute(null))
                {
                    ManageCommand.Execute(null);
                    return;
                }
            }

            // Fallback: Launch Windows Device Manager
            try
            {
                var mgmt = new DeviceManagementService();
                mgmt.LaunchDeviceManager();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceCard] Manage error: {ex.Message}");
            }
        }
    }
}
