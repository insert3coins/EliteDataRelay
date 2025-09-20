; Elite Data Relay Inno Setup Script
; SEE THE INNO SETUP DOCUMENTATION FOR DETAILS ON CREATING SCRIPT FILES!

#define MyAppName "Elite Data Relay"
#define MyAppVersion "0.12.0"
#define MyAppPublisher "insert3coins"
#define MyAppURL "https://github.com/insert3coins/EliteDataRelay"
#define MyAppExeName "EliteDataRelay.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Generate a new GUID for your app if you copy this script for another project.
AppId={{F4C6E1B2-5B7A-4A9E-8F0C-7E6D3A9B1C2D}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE.txt
SetupIconFile=Resources/Appicon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputBaseFilename=EliteDataRelay-{#MyAppVersion}-setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; NOTE: This assumes your application has been built in Release mode.
; It will package the main .exe and all other necessary files from the build output folder.
Source: "bin\Release\net8.0-windows10.0.26100.0\win-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows10.0.26100.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent