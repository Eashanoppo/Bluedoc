# PowerShell script to test instantiating every view in BlueDoc.UI
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

$assemblyPath = "e:\BlueDoc\src\BlueDoc.UI\bin\Release\BlueDoc.exe"
$asm = [System.Reflection.Assembly]::LoadFrom($assemblyPath)

Write-Host "Loaded assembly: $($asm.FullName)" -ForegroundColor Cyan

# Create BlueDoc.UI.App instance so App.xaml is loaded
$appType = $asm.GetType("BlueDoc.UI.App")
if (-not [System.Windows.Application]::Current) {
    $app = [System.Activator]::CreateInstance($appType)
    $app.InitializeComponent()
} else {
    $app = [System.Windows.Application]::Current
}

Write-Host "Loaded App and App.xaml successfully." -ForegroundColor Green

# List of all view types to instantiate
$views = @(
    "BlueDoc.UI.Views.DashboardView",
    "BlueDoc.UI.Views.BluetoothMainView",
    "BlueDoc.UI.Views.BluetoothDevicesView",
    "BlueDoc.UI.Views.DeviceScannerView",
    "BlueDoc.UI.Views.HardwareInspectorView",
    "BlueDoc.UI.Views.DiagnosticCenterView",
    "BlueDoc.UI.Views.CompatibilityView",
    "BlueDoc.UI.Views.SafeRepairCenterView",
    "BlueDoc.UI.Views.DiagnosticReportView",
    "BlueDoc.UI.Views.ConnectionHistoryView",
    "BlueDoc.UI.Views.SignalMonitorView",
    "BlueDoc.UI.Views.BleExplorerView",
    "BlueDoc.UI.Views.SettingsView",
    "BlueDoc.UI.Views.BluetoothEventAnalyzerView",
    "BlueDoc.UI.Views.DriverAssistantView",
    "BlueDoc.UI.Views.AudioDiagnosticsView",
    "BlueDoc.UI.Views.TechnicianToolkitView",
    "BlueDoc.UI.Views.SaasFleetDashboardView",
    "BlueDoc.UI.Views.FixMyConnectionWizardView"
)

$passed = 0
$failed = 0

foreach ($vName in $views) {
    $type = $asm.GetType($vName)
    if ($type -eq $null) {
        Write-Host "TYPE NOT FOUND: $vName" -ForegroundColor Red
        $failed++
        continue
    }

    try {
        $viewInstance = [System.Activator]::CreateInstance($type)
        Write-Host "[PASS] Successfully instantiated: $vName" -ForegroundColor Green
        $passed++
    } catch {
        $ex = $_.Exception
        while ($ex.InnerException) { $ex = $ex.InnerException }
        Write-Host "[FAIL] Exception instantiating $vName : $($ex.Message)" -ForegroundColor Red
        Write-Host "$($ex.StackTrace)" -ForegroundColor Yellow
        $failed++
    }
}

Write-Host "`nSummary: $passed PASSED, $failed FAILED" -ForegroundColor Cyan

# Test MainWindow and MainViewModel navigation across all sections
Write-Host "`nTesting MainWindow and Navigation across all 17 sections..." -ForegroundColor Cyan
try {
    $mainWinType = $asm.GetType("BlueDoc.UI.MainWindow")
    $mainWin = [System.Activator]::CreateInstance($mainWinType)
    Write-Host "[PASS] MainWindow instantiated successfully." -ForegroundColor Green

    $mainVm = $mainWin.DataContext
    $sections = @(
        "Dashboard",
        "Bluetooth",
        "Bluetooth Devices",
        "Device Scanner",
        "Advanced Hardware Inspector",
        "Diagnostic Test Center",
        "Audio & Packet Diagnostics",
        "Bluetooth Event Analyzer",
        "Driver Assistant",
        "Compatibility Checker",
        "Safe Repair Center",
        "Diagnostic Reports",
        "Device Connection History",
        "Signal & Discovery Monitor",
        "Bluetooth LE Explorer",
        "Settings",
        "Technician Toolkit",
        "SaaS Fleet Portal",
        "Fix My Bluetooth Connection"
    )

    foreach ($sec in $sections) {
        $mainVm.NavigateTo($sec)
        $currentVmName = $mainVm.CurrentViewModel.GetType().Name
        Write-Host "  -> Navigated to: '$sec' => CurrentViewModel is $currentVmName" -ForegroundColor Green
    }
    Write-Host "[PASS] All 17 sections navigated without crashing!" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] MainWindow navigation error: $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

if ($failed -gt 0) { exit 1 } else { exit 0 }
