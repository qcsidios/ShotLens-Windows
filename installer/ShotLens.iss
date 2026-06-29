#define ShotLensVersion GetEnv("SHOTLENS_VERSION")
#define ShotLensPublishDir GetEnv("SHOTLENS_PUBLISH_DIR")
#define ShotLensInstallerDir GetEnv("SHOTLENS_INSTALLER_DIR")
#define ShotLensChannel GetEnv("SHOTLENS_CHANNEL")

#if ShotLensChannel == "beta"
  #define ShotLensAppId "{{F6F10CFD-3739-4319-A150-E59C25CA3325}"
  #define ShotLensInstallerName "ShotLens Beta"
  #define ShotLensDefaultDir "{autopf}\ShotLens Beta"
  #define ShotLensOutputPrefix "ShotLens-Beta-"
#else
  #define ShotLensAppId "{{5A7B81D7-4566-4AB5-8A62-2CB0C96F0619}"
  #define ShotLensInstallerName "ShotLens"
  #define ShotLensDefaultDir "{autopf}\ShotLens"
  #define ShotLensOutputPrefix "ShotLens-Windows-"
#endif

[Setup]
AppId={#ShotLensAppId}
AppName={#ShotLensInstallerName}
AppVersion={#ShotLensVersion}
AppPublisher=Qingcheng
AppPublisherURL=https://github.com/qcsidios/ShotLens-Windows
AppSupportURL=https://github.com/qcsidios/ShotLens-Windows/issues
AppUpdatesURL=https://github.com/qcsidios/ShotLens-Windows/releases
DefaultDirName={#ShotLensDefaultDir}
DefaultGroupName={#ShotLensInstallerName}
DisableProgramGroupPage=yes
OutputDir={#ShotLensInstallerDir}
OutputBaseFilename={#ShotLensOutputPrefix}{#ShotLensVersion}-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
SetupIconFile={#ShotLensPublishDir}\ShotLens.ico
UninstallDisplayName=ShotLens
CloseApplications=yes
RestartApplications=yes

[Languages]
Name: "chinesesimp"; MessagesFile: "ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#ShotLensPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#ShotLensInstallerName}"; Filename: "{app}\ShotLens.Windows.App.exe"
Name: "{autodesktop}\{#ShotLensInstallerName}"; Filename: "{app}\ShotLens.Windows.App.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\ShotLens.Windows.App.exe"; Description: "启动 ShotLens"; Flags: nowait postinstall
