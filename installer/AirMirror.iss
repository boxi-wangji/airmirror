#define MyAppName "AirMirror"
#define MyAppVersion GetEnv("AIRMIRROR_VERSION")
#if MyAppVersion == ""
  #define MyAppVersion "1.0.0"
#endif
#define MyAppPublisher "AirMirror"
#define MyAppExeName "AirMirror.exe"
#define SourceRoot AddBackslash(SourcePath) + ".."
#define PublishDir SourceRoot + "\\artifacts\\publish"

[Setup]
AppId={{C7511AA6-7E4C-4B69-9FB8-1B339B0D1F71}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\AirMirror
DefaultGroupName=AirMirror
DisableProgramGroupPage=yes
OutputDir={#SourceRoot}\dist
OutputBaseFilename=AirMirror-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\app\{#MyAppExeName}
LicenseFile={#SourceRoot}\第三方许可.md

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}\app"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourceRoot}\engine\*"; DestDir: "{app}\engine"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourceRoot}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\第三方许可.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\AirMirror"; Filename: "{app}\app\{#MyAppExeName}"
Name: "{autodesktop}\AirMirror"; Filename: "{app}\app\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加选项："

[Run]
Filename: "{app}\app\{#MyAppExeName}"; Description: "启动 AirMirror"; Flags: nowait postinstall skipifsilent
