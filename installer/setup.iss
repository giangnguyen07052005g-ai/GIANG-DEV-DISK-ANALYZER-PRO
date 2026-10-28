; Inno Setup Script for GIANG DEV DISK ANALYZER PRO
; Modern Windows Installer Script

#define MyAppName "GIANG DEV DISK ANALYZER PRO"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "GIANG DEV"
#define MyAppURL "https://giangdev.com"
#define MyAppExeName "GIANG.Dev.DiskAnalyzer.App.exe"

[Setup]
AppId={{E6F74A32-9B7C-4C8D-B6E1-4D561879B5CD}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\GiangDevDiskAnalyzer
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE.txt
OutputDir=..\installer_output
OutputBaseFilename=GiangDev_DiskAnalyzer_Pro_v1.0.0_Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\publish\win-x64\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\win-x64\*.pdb"; DestDir: "{app}"; Flags: ignoreversion; Tasks: 

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
