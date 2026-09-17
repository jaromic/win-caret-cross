; Inno Setup script for Caret Crosshair.
;
; Build (Windows only, requires Inno Setup 6: https://jrsoftware.org/isinfo.php):
;   1. Publish the app first:
;        cd src\CaretCrosshair
;        dotnet publish -c Release
;   2. Compile this script:
;        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\CaretCrosshair.iss
;   Output: installer\dist\CaretCrosshairSetup.exe
;
; This produces a per-user installer (no admin rights required) that places
; the app under %LOCALAPPDATA%\Programs\CaretCrosshair, adds Start Menu +
; uninstall entries (visible in "Apps & Features"), and can register the app
; to launch at sign-in.
;
; NOTE: this script has been authored but not compiled/tested -- ISCC.exe is
; Windows-only and unavailable in the Linux container this was written in.
; First compile + install/uninstall run needs to happen on real Windows.

#define MyAppName "Caret Crosshair"
#define MyAppVersion "1.0.0"
#define MyAppExeName "CaretCrosshair.exe"
#define MyAppMutex "CaretCrosshair.SingleInstance"
#define MyPublishDir "..\src\CaretCrosshair\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"

[Setup]
AppId={{1189F93B-6003-4AA8-8737-67CEDFD93296}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={localappdata}\Programs\CaretCrosshair
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
OutputDir=dist
OutputBaseFilename=CaretCrosshairSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
AppMutex={#MyAppMutex}
CloseApplications=yes
RestartApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startup"; Description: "Start {#MyAppName} automatically when I sign in"; GroupDescription: "Additional options:"

[Files]
Source: "{#MyPublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autoprograms}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName} now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Belt-and-suspenders alongside AppMutex/CloseApplications: make sure a
; running tray instance can't block file removal during uninstall.
Filename: "{cmd}"; Parameters: "/C taskkill /IM {#MyAppExeName} /F"; Flags: runhidden; RunOnceId: "KillCaretCrosshair"
