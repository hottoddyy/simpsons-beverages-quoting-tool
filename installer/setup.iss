[Setup]
; AppId MUST remain this GUID forever — it is how Windows identifies this app
; for upgrades. Changing it would treat every new version as a separate install.
AppId={{F6006416-99D8-4340-B710-F82CED6F0906}
AppName=Simpsons Beverages Quoting Tool
AppVersion=1.3.0
AppPublisher=Simpsons Beverages
DefaultDirName={autopf}\Simpsons Beverages\Quoting Tool
DefaultGroupName=Simpsons Beverages
UninstallDisplayName=Simpsons Beverages Quoting Tool
UninstallDisplayIcon={app}\SimpsonsBeverages.QuotingTool.App.exe
OutputDir=..\publish
OutputBaseFilename=SimpsonsQuotingToolSetup-v1.3.0
SetupIconFile=..\src\SimpsonsBeverages.QuotingTool.App\Assets\simpsons-logo.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
; Upgrade behaviour
UsePreviousAppDir=yes
CloseApplications=yes
CloseApplicationsFilter=*.exe
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
Source: "..\src\SimpsonsBeverages.QuotingTool.App\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Simpsons Beverages\Quoting Tool"; Filename: "{app}\SimpsonsBeverages.QuotingTool.App.exe"; IconFilename: "{app}\Assets\simpsons-logo.ico"
Name: "{autodesktop}\Simpsons Beverages Quoting Tool"; Filename: "{app}\SimpsonsBeverages.QuotingTool.App.exe"; IconFilename: "{app}\Assets\simpsons-logo.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\SimpsonsBeverages.QuotingTool.App.exe"; Description: "Launch Simpsons Beverages Quoting Tool"; Flags: nowait postinstall skipifsilent
