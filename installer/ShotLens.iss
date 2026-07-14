#define ShotLensVersion GetEnv("SHOTLENS_VERSION")
#define ShotLensPublishDir GetEnv("SHOTLENS_PUBLISH_DIR")
#define ShotLensInstallerDir GetEnv("SHOTLENS_INSTALLER_DIR")

[Setup]
AppId={{5A7B81D7-4566-4AB5-8A62-2CB0C96F0619}
AppName=ShotLens
AppVersion={#ShotLensVersion}
AppPublisher=Qingcheng
AppPublisherURL=https://github.com/readercyl/ShotLens-Windows
AppSupportURL=https://github.com/readercyl/ShotLens-Windows/issues
AppUpdatesURL=https://github.com/readercyl/ShotLens-Windows/releases
DefaultDirName={autopf}\ShotLens
DefaultGroupName=ShotLens
DisableProgramGroupPage=yes
OutputDir={#ShotLensInstallerDir}
OutputBaseFilename=ShotLens-Windows-{#ShotLensVersion}-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
SetupIconFile={#ShotLensPublishDir}\ShotLens.ico
UninstallDisplayName=ShotLens

[Languages]
Name: "chinesesimp"; MessagesFile: "ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#ShotLensPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\ShotLens"; Filename: "{app}\ShotLens.Windows.App.exe"
Name: "{autodesktop}\ShotLens"; Filename: "{app}\ShotLens.Windows.App.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\ShotLens.Windows.App.exe"; Description: "启动 ShotLens"; Flags: nowait postinstall skipifsilent
