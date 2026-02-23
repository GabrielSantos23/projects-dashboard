[Setup]
; App Information
AppName=Project Dashboard
AppVersion=1.0.6
AppPublisher=Gabriel Santos
AppPublisherURL=https://github.com/GabrielSantos23/projects-dashboard
AppSupportURL=https://github.com/GabrielSantos23/projects-dashboard/issues
AppUpdatesURL=https://github.com/GabrielSantos23/projects-dashboard/releases

; Installation Directory
DefaultDirName={autopf}\Project Dashboard
DefaultGroupName=Project Dashboard

; Output settings
OutputDir=publish\installer
OutputBaseFilename=ProjectDashboard_Setup
Compression=lzma2
SolidCompression=yes

; Icons and UI
SetupIconFile=Src\DesktopAvalonia\Styles\logo.ico
UninstallDisplayIcon={app}\Project Dashboard.exe
WizardStyle=modern

; Permissions (lowest means it installs for the current user without requiring Admin privileges by default, change to admin if needed)
PrivilegesRequired=lowest

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Copy the main executable and any other published files
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start Menu Icon
Name: "{group}\Project Dashboard"; Filename: "{app}\Project Dashboard.exe"; IconFilename: "{app}\Project Dashboard.exe"
; Desktop Icon
Name: "{autodesktop}\Project Dashboard"; Filename: "{app}\Project Dashboard.exe"; Tasks: desktopicon; IconFilename: "{app}\Project Dashboard.exe"

[Run]
; Launch option at the end of setup
Filename: "{app}\Project Dashboard.exe"; Description: "{cm:LaunchProgram,Project Dashboard}"; Flags: nowait postinstall skipifsilent
