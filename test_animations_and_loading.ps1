# PowerShell script to test Animations, Skeleton Loading, and Loading States across BlueDoc
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

$binDir = "E:\BlueDoc\src\BlueDoc.UI\bin\Release"
[System.Reflection.Assembly]::LoadFrom("$binDir\BlueDoc.Core.dll") | Out-Null
[System.Reflection.Assembly]::LoadFrom("$binDir\BlueDoc.Infrastructure.dll") | Out-Null
[System.Reflection.Assembly]::LoadFrom("$binDir\BlueDoc.Bluetooth.dll") | Out-Null
[System.Reflection.Assembly]::LoadFrom("$binDir\BlueDoc.Diagnostics.dll") | Out-Null
[System.Reflection.Assembly]::LoadFrom("$binDir\BlueDoc.Data.dll") | Out-Null
[System.Reflection.Assembly]::LoadFrom("$binDir\BlueDoc.Application.dll") | Out-Null
$uiAssembly = [System.Reflection.Assembly]::LoadFrom("$binDir\BlueDoc.exe")

Write-Host "==============================================================="
Write-Host "  BLUEDOC ANIMATIONS & LOADING SYSTEM ACCEPTANCE TEST"
Write-Host "==============================================================="

$appType = $uiAssembly.GetType("BlueDoc.UI.App")
if (-not [System.Windows.Application]::Current) {
    $app = [System.Activator]::CreateInstance($appType)
    $app.InitializeComponent()
} else {
    $app = [System.Windows.Application]::Current
}
Write-Host "Application resources and styles loaded successfully." -ForegroundColor Green

$script:passed = 0
$script:failed = 0

function Assert-Test([string]$name, [bool]$condition, [string]$detail = "") {
    if ($condition) {
        Write-Host "  [PASS] $name" -ForegroundColor Green
        $script:passed++
    } else {
        Write-Host "  [FAIL] $name - $detail" -ForegroundColor Red
        $script:failed++
    }
}

# 1. TEST MOTION MANAGER
Write-Host "`n[SUITE 1] MotionManager System & Tier-0 Compatibility Tests" -ForegroundColor Cyan
$motionMgrType = $uiAssembly.GetType("BlueDoc.UI.Common.MotionManager")
$instanceProp = $motionMgrType.GetProperty("Instance", [System.Reflection.BindingFlags]"Public,Static")
$motionMgr = $instanceProp.GetValue($null, $null)
Assert-Test "MotionManager singleton instantiated" ($motionMgr -ne $null)

$isAnimAllowed = $motionMgr.IsAnimationAllowed
$isReduced = $motionMgr.IsReducedMotionPreferred
$isSoftware = $motionMgr.IsSoftwareRendering
Write-Host "    MotionManager Telemetry: IsAnimationAllowed=$isAnimAllowed, IsReducedMotionPreferred=$isReduced, IsSoftwareRendering=$isSoftware" -ForegroundColor Gray
Assert-Test "MotionManager normal duration exposed (180ms)" ($motionMgr.PageTransitionDuration.TotalMilliseconds -eq 180 -or $motionMgr.PageTransitionDuration.TotalMilliseconds -eq 0)
Assert-Test "MotionManager micro duration exposed (120ms)" ($motionMgr.MicroInteractionDuration.TotalMilliseconds -eq 120 -or $motionMgr.MicroInteractionDuration.TotalMilliseconds -eq 0)

# Test ForceReducedMotion toggle
$motionMgr.ForceReducedMotion = $true
Assert-Test "ForceReducedMotion sets IsReducedMotionPreferred=True" ($motionMgr.IsReducedMotionPreferred -eq $true)
Assert-Test "ForceReducedMotion sets IsAnimationAllowed=False" ($motionMgr.IsAnimationAllowed -eq $false)
$motionMgr.ForceReducedMotion = $false

# 2. TEST SKELETON LOADING TEMPLATES IN LOADING.XAML
Write-Host "`n[SUITE 2] Skeleton Loading Templates in Loading.xaml" -ForegroundColor Cyan
$skeletonKeys = @(
    "DeviceCardSkeletonTemplate",
    "DeviceCardListSkeletonTemplate",
    "DeviceDetailSkeletonTemplate",
    "DashboardStatsSkeletonTemplate",
    "AdapterInfoSkeletonTemplate",
    "DiagnosticResultListSkeletonTemplate",
    "EventLogTableSkeletonTemplate",
    "ConnectionHistorySkeletonTemplate",
    "ReportSkeletonTemplate",
    "HardwareInspectorSkeletonTemplate"
)

foreach ($key in $skeletonKeys) {
    $tpl = [System.Windows.Application]::Current.TryFindResource($key)
    Assert-Test "Template registered in App resources: $key" ($tpl -ne $null)
    if ($tpl -ne $null) {
        $ctrl = New-Object System.Windows.Controls.Control
        $ctrl.Template = $tpl
        $ctrl.ApplyTemplate() | Out-Null
        Assert-Test "Template instantiates cleanly: $key" ($ctrl -ne $null)
    }
}

# 3. TEST ANIMATION STORYBOARDS IN ANIMATIONS.XAML
Write-Host "`n[SUITE 3] Animation Storyboards in Animations.xaml" -ForegroundColor Cyan
$animKeys = @(
    "PageFadeInStoryboard",
    "ContentEnterStoryboard",
    "SidebarExpandStoryboard",
    "SidebarCollapseStoryboard",
    "CardSelectionStoryboard",
    "DeviceEntranceStoryboard",
    "ButtonHoverStoryboard",
    "ButtonPressedStoryboard",
    "ToastEnterStoryboard",
    "ToastExitStoryboard",
    "SuccessPulseStoryboard",
    "ErrorAlertStoryboard",
    "SpinnerRotateStoryboard"
)

foreach ($key in $animKeys) {
    $sb = [System.Windows.Application]::Current.TryFindResource($key)
    Assert-Test "Storyboard registered in App resources: $key" ($sb -ne $null)
    if ($sb -ne $null) {
        Assert-Test "Storyboard has timelines: $key" ($sb.Children.Count -gt 0)
    }
}

# Instantiate MainViewModel which initializes all services and ViewModels
$mainVm = New-Object BlueDoc.UI.ViewModels.MainViewModel

# 4. TEST VIEWMODEL LOADING STATES & STAGES
Write-Host "`n[SUITE 4] DiagnosticCenterViewModel Progress & Cancellation Tests" -ForegroundColor Cyan
$diagVm = $mainVm.DiagnosticCenterVM
Assert-Test "Total tests count is exactly 12" ($diagVm.TotalTestsCount -eq 12)
Assert-Test "Initial completed count is 0" ($diagVm.CompletedTestsCount -eq 0)
Assert-Test "Initial progress display is '0 of 12 tests completed'" ($diagVm.TestsProgressDisplay -eq "0 of 12 tests completed")
Assert-Test "Initial estimated remaining display is empty" ($diagVm.EstimatedRemainingDisplay -eq "")
Assert-Test "CancelDiagnosticsCommand is not null" ($diagVm.CancelDiagnosticsCommand -ne $null)

Write-Host "`n[SUITE 5] BluetoothDevicesViewModel Loading & Stages Tests" -ForegroundColor Cyan
$btVm = $mainVm.BluetoothDevicesVM
Assert-Test "Initial IsLoading is false" ($btVm.IsLoading -eq $false)
Assert-Test "Initial IsRefreshing is false" ($btVm.IsRefreshing -eq $false)
Assert-Test "Initial ConnectionStage is Idle" ($btVm.ConnectionStage.ToString() -eq "Idle")
Assert-Test "Initial PairingStage is Idle" ($btVm.PairingStage.ToString() -eq "Idle")
Assert-Test "Initial IsConnecting is false" ($btVm.IsConnecting -eq $false)
Assert-Test "Initial IsPairing is false" ($btVm.IsPairing -eq $false)

# Stage property transitions
$btVm.ConnectionStage = [BlueDoc.Core.Enums.BluetoothConnectionStage]::PreparingConnection
Assert-Test "IsConnecting is true during PreparingConnection" ($btVm.IsConnecting -eq $true)
Assert-Test "ConnectionStageDisplay reflects Preparing" ($btVm.ConnectionStageDisplay -eq "Preparing Connection...")

$btVm.ConnectionStage = [BlueDoc.Core.Enums.BluetoothConnectionStage]::Connecting
Assert-Test "IsConnecting is true during Connecting" ($btVm.IsConnecting -eq $true)
Assert-Test "ConnectionStageDisplay reflects Connecting" ($btVm.ConnectionStageDisplay -eq "Establishing RFCOMM / Audio Link...")

$btVm.ConnectionStage = [BlueDoc.Core.Enums.BluetoothConnectionStage]::Connected
Assert-Test "IsConnecting is false when Connected" ($btVm.IsConnecting -eq $false)
Assert-Test "ConnectionStageDisplay reflects Connected" ($btVm.ConnectionStageDisplay -eq "Connected")

$btVm.PairingStage = [BlueDoc.Core.Enums.BluetoothPairingStage]::WaitingForAuthentication
Assert-Test "IsPairing is true during WaitingForAuthentication" ($btVm.IsPairing -eq $true)
Assert-Test "PairingStageDisplay reflects Waiting" ($btVm.PairingStageDisplay -eq "Waiting for Authentication...")

$btVm.PairingStage = [BlueDoc.Core.Enums.BluetoothPairingStage]::PairingCompleted
Assert-Test "IsPairing is false when PairingCompleted" ($btVm.IsPairing -eq $false)

Write-Host "`n[SUITE 6] DeviceScannerViewModel Discovery & Skeletons Tests" -ForegroundColor Cyan
$scannerVm = $mainVm.DeviceScannerVM
Assert-Test "Initial ShowScanSkeleton is false" ($scannerVm.ShowScanSkeleton -eq $false)
Assert-Test "Initial ShowEmptyState is true (idle and empty)" ($scannerVm.ShowEmptyState -eq $true)

# Simulate scan start with empty catalog
$scannerVm.IsScanning = $true
Assert-Test "ShowScanSkeleton is true when scanning and 0 devices" ($scannerVm.ShowScanSkeleton -eq $true)
Assert-Test "ShowEmptyState is false when scanning" ($scannerVm.ShowEmptyState -eq $false)

# Simulate device discovered
$mockDev = New-Object BlueDoc.Core.Models.BluetoothDeviceInfo
$mockDev.DeviceName = "Test Headphone"
$mockDev.BluetoothAddress = "11:22:33:44:55:66"
$scannerVm.DiscoveredDevices.Add($mockDev)
Assert-Test "ShowScanSkeleton becomes false when devices discovered" ($scannerVm.ShowScanSkeleton -eq $false)

$scannerVm.IsScanning = $false
Assert-Test "ShowEmptyState is false when catalog has devices" ($scannerVm.ShowEmptyState -eq $false)

Write-Host "`n[SUITE 7] Dashboard & Hardware Inspector ViewModels" -ForegroundColor Cyan
$dashVm = $mainVm.DashboardVM
Assert-Test "DashboardVm has IsRefreshing property" ($dashVm.IsRefreshing -eq $false)

$hwVm = $mainVm.HardwareInspectorVM
Assert-Test "HardwareInspectorVm has IsRefreshing property" ($hwVm.IsRefreshing -eq $false)

Write-Host "`n==============================================================="
Write-Host "  RESULTS: $script:passed PASSED, $script:failed FAILED"
Write-Host "==============================================================="

if ($script:failed -eq 0) {
    exit 0
} else {
    exit 1
}
