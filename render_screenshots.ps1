# render_screenshots.ps1
# Comprehensive off-screen high-resolution renderer for all BlueDoc views in Dark and Light themes
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

$assemblyPath = "e:\BlueDoc\src\BlueDoc.UI\bin\Release\BlueDoc.exe"
$asm = [System.Reflection.Assembly]::LoadFrom($assemblyPath)

$outputDir = "e:\BlueDoc\screenshots"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$appType = $asm.GetType("BlueDoc.UI.App")
if (-not [System.Windows.Application]::Current) {
    $app = [System.Activator]::CreateInstance($appType)
    $app.InitializeComponent()
} else {
    $app = [System.Windows.Application]::Current
}

$mainVm = [BlueDoc.UI.ViewModels.MainViewModel]::new()

# Pre-load async data for core ViewModels
try {
    $t1 = $mainVm.DashboardVM.LoadDataAsync(); if ($t1) { $t1.Wait() }
    $mainVm.DashboardVM.IsLoading = $false
    $t2 = $mainVm.BluetoothMainVM.LoadDataAsync(); if ($t2) { $t2.Wait() }
    $mainVm.BluetoothMainVM.IsLoading = $false
} catch {}

$renderWidth = 1280
$renderHeight = 850

function Render-View($viewType, $vm, $filePath, $isLight = $false) {
    $view = [System.Activator]::CreateInstance($viewType)
    $view.DataContext = $vm

    # Create root container to host background canvas
    $container = New-Object System.Windows.Controls.Border
    $bgBrushName = if ($isLight) { "SurfaceCanvasBrush" } else { "BgDarkBrush" }
    $brush = [System.Windows.Application]::Current.Resources[$bgBrushName]
    if ($brush) { $container.Background = $brush }
    $container.Child = $view

    $container.Measure([System.Windows.Size]::new($renderWidth, $renderHeight))
    $container.Arrange([System.Windows.Rect]::new(0, 0, $renderWidth, $renderHeight))
    $container.UpdateLayout()

    $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap($renderWidth, $renderHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $rtb.Render($container)

    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))

    $stream = [System.IO.File]::Create($filePath)
    $encoder.Save($stream)
    $stream.Close()
    
    $len = (Get-Item $filePath).Length
    Write-Host "  [RENDERED] $([System.IO.Path]::GetFileName($filePath)) ($([math]::Round($len / 1024, 1)) KB)" -ForegroundColor Green
}

function Render-FullWindow($sectionKey, $filePath) {
    $winType = $asm.GetType("BlueDoc.UI.MainWindow")
    $win = [System.Activator]::CreateInstance($winType)
    $vm = $win.DataContext
    try {
        $t1 = $vm.DashboardVM.LoadDataAsync(); if ($t1) { $t1.Wait() }
        $vm.DashboardVM.IsLoading = $false
        $t2 = $vm.BluetoothMainVM.LoadDataAsync(); if ($t2) { $t2.Wait() }
        $vm.BluetoothMainVM.IsLoading = $false
    } catch {}
    $vm.NavigateTo($sectionKey)

    $rootVisual = $win.Content -as [System.Windows.FrameworkElement]
    $rootVisual.Measure([System.Windows.Size]::new(1366, 820))
    $rootVisual.Arrange([System.Windows.Rect]::new(0, 0, 1366, 820))
    $rootVisual.UpdateLayout()

    $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(1366, 820, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $rtb.Render($rootVisual)

    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))

    $stream = [System.IO.File]::Create($filePath)
    $encoder.Save($stream)
    $stream.Close()
    
    $len = (Get-Item $filePath).Length
    Write-Host "  [WINDOW] $([System.IO.Path]::GetFileName($filePath)) ($([math]::Round($len / 1024, 1)) KB)" -ForegroundColor Cyan
}

$screenDefinitions = @(
    @{ Name = "00_BluetoothMain"; View = "BlueDoc.UI.Views.BluetoothMainView"; Key = "Bluetooth" },
    @{ Name = "01_Dashboard"; View = "BlueDoc.UI.Views.DashboardView"; Key = "Dashboard" },
    @{ Name = "02_BluetoothDevices"; View = "BlueDoc.UI.Views.BluetoothDevicesView"; Key = "Bluetooth Devices" },
    @{ Name = "03_DeviceScanner"; View = "BlueDoc.UI.Views.DeviceScannerView"; Key = "Device Scanner" },
    @{ Name = "04_HardwareInspector"; View = "BlueDoc.UI.Views.HardwareInspectorView"; Key = "Advanced Hardware Inspector" },
    @{ Name = "05_DiagnosticCenter"; View = "BlueDoc.UI.Views.DiagnosticCenterView"; Key = "Diagnostic Test Center" },
    @{ Name = "06_AudioDiagnostics"; View = "BlueDoc.UI.Views.AudioDiagnosticsView"; Key = "Audio & Packet Diagnostics" },
    @{ Name = "07_EventAnalyzer"; View = "BlueDoc.UI.Views.BluetoothEventAnalyzerView"; Key = "Bluetooth Event Analyzer" },
    @{ Name = "08_DriverAssistant"; View = "BlueDoc.UI.Views.DriverAssistantView"; Key = "Driver Assistant" },
    @{ Name = "09_CompatibilityChecker"; View = "BlueDoc.UI.Views.CompatibilityView"; Key = "Compatibility Checker" },
    @{ Name = "10_SafeRepairCenter"; View = "BlueDoc.UI.Views.SafeRepairCenterView"; Key = "Safe Repair Center" },
    @{ Name = "11_DiagnosticReports"; View = "BlueDoc.UI.Views.DiagnosticReportView"; Key = "Diagnostic Reports" },
    @{ Name = "12_ConnectionHistory"; View = "BlueDoc.UI.Views.ConnectionHistoryView"; Key = "Device Connection History" },
    @{ Name = "13_SignalMonitor"; View = "BlueDoc.UI.Views.SignalMonitorView"; Key = "Signal & Discovery Monitor" },
    @{ Name = "14_BleExplorer"; View = "BlueDoc.UI.Views.BleExplorerView"; Key = "Bluetooth LE Explorer" },
    @{ Name = "15_TechnicianToolkit"; View = "BlueDoc.UI.Views.TechnicianToolkitView"; Key = "Technician Toolkit" },
    @{ Name = "16_SaasFleetPortal"; View = "BlueDoc.UI.Views.SaasFleetDashboardView"; Key = "SaaS Fleet Portal" },
    @{ Name = "17_Settings"; View = "BlueDoc.UI.Views.SettingsView"; Key = "Settings" },
    @{ Name = "18_FixMyConnectionWizard"; View = "BlueDoc.UI.Views.FixMyConnectionWizardView"; Key = "Fix My Bluetooth Connection" }
)

Write-Host "Rendering all BlueDoc views in Dark Theme..." -ForegroundColor Cyan
[BlueDoc.UI.Common.ThemeManager]::SetTheme([BlueDoc.UI.Common.AppTheme]::Dark)

foreach ($item in $screenDefinitions) {
    $mainVm.NavigateTo($item.Key)
    $viewType = $asm.GetType($item.View)
    $filePath = Join-Path $outputDir "$($item.Name).png"
    Render-View $viewType $mainVm.CurrentViewModel $filePath $false
}

Write-Host "`nRendering Full Window Shell in Dark Theme..." -ForegroundColor Cyan
Render-FullWindow "Bluetooth" (Join-Path $outputDir "00_MainWindow_Bluetooth_Dark.png")
Render-FullWindow "Dashboard" (Join-Path $outputDir "01_MainWindow_Dashboard_Dark.png")

Write-Host "`nRendering key views in Light Theme..." -ForegroundColor Cyan
[BlueDoc.UI.Common.ThemeManager]::SetTheme([BlueDoc.UI.Common.AppTheme]::Light)

$lightItems = @(
    @{ Name = "00_BluetoothMain_Light"; View = "BlueDoc.UI.Views.BluetoothMainView"; Key = "Bluetooth" },
    @{ Name = "01_Dashboard_Light"; View = "BlueDoc.UI.Views.DashboardView"; Key = "Dashboard" },
    @{ Name = "04_HardwareInspector_Light"; View = "BlueDoc.UI.Views.HardwareInspectorView"; Key = "Advanced Hardware Inspector" },
    @{ Name = "05_DiagnosticCenter_Light"; View = "BlueDoc.UI.Views.DiagnosticCenterView"; Key = "Diagnostic Test Center" },
    @{ Name = "06_AudioDiagnostics_Light"; View = "BlueDoc.UI.Views.AudioDiagnosticsView"; Key = "Audio & Packet Diagnostics" },
    @{ Name = "10_SafeRepairCenter_Light"; View = "BlueDoc.UI.Views.SafeRepairCenterView"; Key = "Safe Repair Center" },
    @{ Name = "17_Settings_Light"; View = "BlueDoc.UI.Views.SettingsView"; Key = "Settings" },
    @{ Name = "18_FixMyConnectionWizard_Light"; View = "BlueDoc.UI.Views.FixMyConnectionWizardView"; Key = "Fix My Bluetooth Connection" }
)

foreach ($item in $lightItems) {
    $mainVm.NavigateTo($item.Key)
    $viewType = $asm.GetType($item.View)
    $filePath = Join-Path $outputDir "$($item.Name).png"
    Render-View $viewType $mainVm.CurrentViewModel $filePath $true
}

Write-Host "`nRendering Full Window Shell in Light Theme..." -ForegroundColor Cyan
Render-FullWindow "Bluetooth" (Join-Path $outputDir "00_MainWindow_Bluetooth_Light.png")

# Revert to Dark Theme
[BlueDoc.UI.Common.ThemeManager]::SetTheme([BlueDoc.UI.Common.AppTheme]::Dark)
Write-Host "`nAll screenshots rendered successfully!" -ForegroundColor Green
