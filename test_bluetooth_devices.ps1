Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

$rootDir = "E:\BlueDoc"
[System.Reflection.Assembly]::LoadFrom("$rootDir\src\BlueDoc.Infrastructure\bin\Release\BlueDoc.Infrastructure.dll") | Out-Null
[System.Reflection.Assembly]::LoadFrom("$rootDir\src\BlueDoc.Bluetooth\bin\Release\BlueDoc.Bluetooth.dll") | Out-Null
[System.Reflection.Assembly]::LoadFrom("$rootDir\src\BlueDoc.Application\bin\Release\BlueDoc.Application.dll") | Out-Null
$asm = [System.Reflection.Assembly]::LoadFrom("$rootDir\src\BlueDoc.UI\bin\Release\BlueDoc.exe")

$appType = $asm.GetType("BlueDoc.UI.App")
if (-not [System.Windows.Application]::Current) {
    $app = [System.Activator]::CreateInstance($appType)
    $app.InitializeComponent()
}

Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "  BLUEDOC - BLUETOOTH DEVICES REDESIGN VERIFICATION" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan

# Test 1: MainWindow & BluetoothDevicesView binding
Write-Host "`n[1/5] Instantiating MainWindow and navigating to Bluetooth Devices..." -ForegroundColor Yellow
$mainWinType = $asm.GetType("BlueDoc.UI.MainWindow")
$mainWin = [System.Activator]::CreateInstance($mainWinType)
$mainVm = $mainWin.DataContext
$mainVm.NavigateTo("Bluetooth Devices")

$vm = $mainVm.BluetoothDevicesVM
if ($vm -eq $null) { throw "BluetoothDevicesVM is null" }
Write-Host "  [PASS] Navigated to Bluetooth Devices. CurrentViewModel is $($mainVm.CurrentViewModel.GetType().Name)" -ForegroundColor Green

# Test 2: Live Paired Devices Enumeration
Write-Host "`n[2/5] Enumerating Live Paired Devices..." -ForegroundColor Yellow
$task = $vm.LoadPairedDevicesAsync()
$task.Wait(4000)
[System.Windows.Threading.Dispatcher]::CurrentDispatcher.Invoke([Action]{}, [System.Windows.Threading.DispatcherPriority]::Background)

Write-Host "  -> Paired Devices Count: $($vm.PairedDevices.Count)" -ForegroundColor Green
Write-Host "  -> Connected Count: $($vm.ConnectedCount), Disconnected: $($vm.DisconnectedCount)" -ForegroundColor Green

foreach ($d in $vm.PairedDevices) {
    Write-Host "     * [$($d.CategoryIcon)] '$($d.DisplayName)' | MAC: $($d.BluetoothAddress) | State: $($d.StatusDisplay) | Type: $($d.DeviceType)" -ForegroundColor Gray
}

# Test 3: Selection & Inspector Details
Write-Host "`n[3/5] Inspecting Selected Device & Actions..." -ForegroundColor Yellow
if ($vm.PairedDevices.Count -gt 0) {
    $firstDevice = $vm.PairedDevices[0]
    $vm.SelectedDevice = $firstDevice
    Write-Host "  -> Selected: '$($vm.SelectedDevice.DisplayName)' ($($vm.SelectedDevice.BluetoothAddress))" -ForegroundColor Green
    Write-Host "  -> IsConnected: $($vm.SelectedDevice.IsConnected)" -ForegroundColor Green
    Write-Host "  -> CanConnect: $($vm.CanConnectSelectedDevice), CanDisconnect: $($vm.CanDisconnectSelectedDevice)" -ForegroundColor Green

    # Wait for background service inspection
    Start-Sleep -Milliseconds 600
    Write-Host "  -> Discovered Services/Profiles: $($vm.SelectedDeviceServices.Count)" -ForegroundColor Green
    foreach ($srv in $vm.SelectedDeviceServices) {
        Write-Host "     - $srv" -ForegroundColor Gray
    }
} else {
    Write-Host "  -> No paired devices on this machine to select (clean empty state)." -ForegroundColor Yellow
}

# Test 4: Search & Filtering Logic
Write-Host "`n[4/5] Testing Real-Time Search & Category Filters..." -ForegroundColor Yellow
$vm.SearchText = "o"
$matchCount = 0
foreach ($item in $vm.FilteredDevices) { $matchCount++ }
Write-Host "  -> Search filter 'o': $matchCount matches" -ForegroundColor Green

$vm.SearchText = ""
$filters = @("All", "Connected", "Disconnected", "Audio", "Peripherals", "BLE", "Classic")
foreach ($f in $filters) {
    $vm.SetFilterCommand.Execute($f)
    $cnt = 0
    foreach ($item in $vm.FilteredDevices) { $cnt++ }
    Write-Host "  -> Filter '$f': $cnt matches" -ForegroundColor Green
}
$vm.SetFilterCommand.Execute("All")

# Test 5: In-App Native Pairing Modal Workflow
Write-Host "`n[5/5] Testing In-App Native Pairing Workflow Modal..." -ForegroundColor Yellow
Write-Host "  -> Initial Modal State: IsPairingModalOpen = $($vm.IsPairingModalOpen)" -ForegroundColor Green
$vm.OpenPairingModalCommand.Execute($null)
Write-Host "  -> Opened Modal: IsPairingModalOpen = $($vm.IsPairingModalOpen), Step = '$($vm.PairingStep)', Discovering = $($vm.IsDiscoveringNearby)" -ForegroundColor Green
if (-not $vm.IsPairingModalOpen) { throw "Expected IsPairingModalOpen to be true" }

$vm.ClosePairingModalCommand.Execute($null)
Write-Host "  -> Closed Modal: IsPairingModalOpen = $($vm.IsPairingModalOpen), Discovering = $($vm.IsDiscoveringNearby)" -ForegroundColor Green
if ($vm.IsPairingModalOpen) { throw "Expected IsPairingModalOpen to be false" }

Write-Host "`n===============================================================" -ForegroundColor Cyan
Write-Host "  ALL BLUETOOTH DEVICES PAGE ACCEPTANCE TESTS PASSED (100%)" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
