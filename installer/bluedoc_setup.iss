; ==============================================================================
; BlueDoc Windows Setup Script (Inno Setup 6+)
; Product: BlueDoc - Windows Bluetooth Diagnostics & Management
; Company: UNLEFT LLC
; Developer: Golam Morshed Eashan
; Tagline: A Bluetooth Doctor for Windows
; Mission: Detect the problem. Understand the cause. Guide the repair.
; ==============================================================================

#define MyAppName "BlueDoc"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "UNLEFT LLC"
#define MyAppDeveloper "Golam Morshed Eashan"
#define MyAppURL "https://unleft.com"
#define MyAppExeName "BlueDoc.exe"
#define SourceBinPath "..\src\BlueDoc.UI\bin\Release"

[Setup]
AppId={{D37E6924-E859-4D89-A2C3-5A384A9E2190}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\dist
OutputBaseFilename=BlueDoc_v1.0.0_Setup
SetupIconFile=..\src\BlueDoc.UI\BlueDoc.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=6.1sp1
UninstallDisplayIcon={app}\BlueDoc.ico
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=BlueDoc
VersionInfoCopyright=Copyright (C) 2026 UNLEFT LLC. All rights reserved.

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1

[Files]
; Primary Executable and Configuration
Source: "{#SourceBinPath}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceBinPath}\{#MyAppExeName}.config"; DestDir: "{app}"; Flags: ignoreversion

; Core and Layer Assembly DLLs
Source: "{#SourceBinPath}\BlueDoc.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceBinPath}\BlueDoc.Infrastructure.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceBinPath}\BlueDoc.Bluetooth.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceBinPath}\BlueDoc.Diagnostics.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceBinPath}\BlueDoc.Data.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceBinPath}\BlueDoc.Application.dll"; DestDir: "{app}"; Flags: ignoreversion

; SQLite Managed Provider
Source: "{#SourceBinPath}\System.Data.SQLite.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceBinPath}\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion

; SQLite Native Interop Binaries (x86 and x64)
Source: "{#SourceBinPath}\x86\SQLite.Interop.dll"; DestDir: "{app}\x86"; Flags: ignoreversion
Source: "{#SourceBinPath}\x64\SQLite.Interop.dll"; DestDir: "{app}\x64"; Flags: ignoreversion

; Application Icon
Source: "..\src\BlueDoc.UI\BlueDoc.ico"; DestDir: "{app}"; Flags: ignoreversion

; Documentation
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme; DestName: "README.txt"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\BlueDoc.ico"; Comment: "BlueDoc"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\BlueDoc.ico"; Comment: "BlueDoc"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Verify .NET Framework 4.7.1 or higher is present (Release key >= 461308)
function IsDotNetSupported(): Boolean;
var
  installedRelease: Cardinal;
begin
  Result := false;
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', installedRelease) then
  begin
    // 461308 corresponds to .NET Framework 4.7.1
    // 528040 corresponds to .NET Framework 4.8
    if installedRelease >= 461308 then
      Result := true;
  end;
end;

function InitializeSetup(): Boolean;
begin
  if not IsDotNetSupported() then
  begin
    MsgBox('BlueDoc requires Microsoft .NET Framework 4.7.1 or higher.' + #13#10 +
           'Please install the .NET Framework 4.8 runtime and rerun setup.' + #13#10 + #13#10 +
           'Download: https://dotnet.microsoft.com/download/dotnet-framework/net48', mbCriticalError, MB_OK);
    Result := false;
  end
  else
  begin
    Result := true;
  end;
end;

