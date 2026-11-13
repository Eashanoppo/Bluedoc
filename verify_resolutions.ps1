# verify_resolutions.ps1
# Multi-resolution rendering and layout inspection for BlueDoc UI
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

$assemblyPath = "e:\BlueDoc\src\BlueDoc.UI\bin\Release\BlueDoc.exe"
$asm = [System.Reflection.Assembly]::LoadFrom($assemblyPath)

$outputDir = "e:\BlueDoc\screenshots\resolutions"
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

$resolutions = @(
    @{ Width = 1280; Height = 720; Tag = "1280x720" },
    @{ Width = 1366; Height = 768; Tag = "1366x768" },
    @{ Width = 1600; Height = 900; Tag = "1600x900" },
    @{ Width = 1920; Height = 1080; Tag = "1920x1080" }
)

$targetViews = @(
    @{ Name = "01_Dashboard"; View = "BlueDoc.UI.Views.DashboardView"; Key = "Dashboard" },
    @{ Name = "02_BluetoothDevices"; View = "BlueDoc.UI.Views.BluetoothDevicesView"; Key = "Bluetooth Devices" },
    @{ Name = "03_DeviceScanner"; View = "BlueDoc.UI.Views.DeviceScannerView"; Key = "Device Scanner" },
    @{ Name = "05_DiagnosticCenter"; View = "BlueDoc.UI.Views.DiagnosticCenterView"; Key = "Diagnostic Test Center" },
    @{ Name = "06_AudioDiagnostics"; View = "BlueDoc.UI.Views.AudioDiagnosticsView"; Key = "Audio & Packet Diagnostics" },
    @{ Name = "10_SafeRepairCenter"; View = "BlueDoc.UI.Views.SafeRepairCenterView"; Key = "Safe Repair Center" },
    @{ Name = "18_FixMyConnectionWizard"; View = "BlueDoc.UI.Views.FixMyConnectionWizardView"; Key = "Fix My Bluetooth Connection" }
)

function Render-ResolutionView($viewType, $vm, $width, $height, $filePath, $isLight = $false) {
    $view = [System.Activator]::CreateInstance($viewType)
    $view.DataContext = $vm

    $container = New-Object System.Windows.Controls.Border
    $bgBrushName = if ($isLight) { "SurfaceCanvasBrush" } else { "BgDarkBrush" }
    $brush = [System.Windows.Application]::Current.Resources[$bgBrushName]
    if ($brush) { $container.Background = $brush }
    $container.Child = $view

    $container.Measure([System.Windows.Size]::new($width, $height))
    $container.Arrange([System.Windows.Rect]::new(0, 0, $width, $height))
    $container.UpdateLayout()

    $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap($width, $height, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $rtb.Render($container)

    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))

    $stream = [System.IO.File]::Create($filePath)
    $encoder.Save($stream)
    $stream.Close()
    
    $len = (Get-Item $filePath).Length
    return [math]::Round($len / 1024, 1)
}

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  BLUEDOC MULTI-RESOLUTION VISUAL VERIFICATION HARNESS" -ForegroundColor Cyan
Write-Host "  Target Resolutions: 1280x720, 1366x768, 1600x900, 1920x1080" -ForegroundColor Cyan
Write-Host "=================================================================`n" -ForegroundColor Cyan

[BlueDoc.UI.Common.ThemeManager]::SetTheme([BlueDoc.UI.Common.AppTheme]::Dark)

$renderedCount = 0
foreach ($res in $resolutions) {
    Write-Host "[RESOLUTION] Testing $($res.Tag) ($($res.Width) x $($res.Height))..." -ForegroundColor Yellow
    foreach ($item in $targetViews) {
        $mainVm.NavigateTo($item.Key)
        $viewType = $asm.GetType($item.View)
        $fileName = "$($res.Tag)_$($item.Name).png"
        $filePath = Join-Path $outputDir $fileName
        $kb = Render-ResolutionView $viewType $mainVm.CurrentViewModel $res.Width $res.Height $filePath $false
        Write-Host "  -> Rendered: $fileName ($kb KB)" -ForegroundColor Green
        $renderedCount++
    }
}

# Also test Light Theme on 1366x768 and 1920x1080
Write-Host "`n[LIGHT THEME] Testing Light Theme on standard resolutions (1366x768 & 1920x1080)..." -ForegroundColor Yellow
[BlueDoc.UI.Common.ThemeManager]::SetTheme([BlueDoc.UI.Common.AppTheme]::Light)

$lightRes = @(
    @{ Width = 1366; Height = 768; Tag = "1366x768" },
    @{ Width = 1920; Height = 1080; Tag = "1920x1080" }
)

foreach ($res in $lightRes) {
    foreach ($item in $targetViews) {
        $mainVm.NavigateTo($item.Key)
        $viewType = $asm.GetType($item.View)
        $fileName = "$($res.Tag)_$($item.Name)_Light.png"
        $filePath = Join-Path $outputDir $fileName
        $kb = Render-ResolutionView $viewType $mainVm.CurrentViewModel $res.Width $res.Height $filePath $true
        Write-Host "  -> Rendered: $fileName ($kb KB)" -ForegroundColor Green
        $renderedCount++
    }
}

# Revert to Dark Theme
[BlueDoc.UI.Common.ThemeManager]::SetTheme([BlueDoc.UI.Common.AppTheme]::Dark)

Write-Host "`n[SUCCESS] Completed multi-resolution visual verification. Total images rendered: $renderedCount" -ForegroundColor Cyan

