#define MyAppName "AirMirror"
#define MyAppVersion GetEnv("AIRMIRROR_VERSION")
#if MyAppVersion == ""
  #define MyAppVersion "1.0.0"
#endif
#define MyAppPublisher "AirMirror"
#define MyAppExeName "AirMirror.exe"
#define SourceRoot AddBackslash(SourcePath) + ".."
#define PublishDir SourceRoot + "\\artifacts\\publish"
#define AssetsDir SourceRoot + "\\assets"

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
SetupIconFile={#AssetsDir}\AirMirror.ico
WizardImageFile={#SourceRoot}\installer\airmirror-wizard.bmp
WizardSmallImageFile={#SourceRoot}\installer\airmirror-wizard-small.bmp
LicenseFile={#SourceRoot}\第三方许可.md
ShowLanguageDialog=no
LanguageDetectionMethod=locale
DisableWelcomePage=no

[Languages]
Name: "chinesesimplified"; MessagesFile: "languages\ChineseSimplified.isl"

[CustomMessages]
chinesesimplified.WelcomeLabel1=欢迎使用 AirMirror 安装向导
chinesesimplified.WelcomeLabel2=AirMirror 让 iPhone 投屏到 Windows 更简单。建议先退出正在运行的 AirMirror，再继续安装。
chinesesimplified.SelectTasksLabel2=请选择要执行的附加任务，然后点击“下一步”。
chinesesimplified.CreateDesktopIcon=创建桌面快捷方式
chinesesimplified.LaunchProgram=立即启动 AirMirror
chinesesimplified.FinishedHeadingLabel=AirMirror 已准备就绪
chinesesimplified.FinishedLabel=安装已完成。你可以现在启动 AirMirror，并填写一个接收设备名后开始投屏。

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}\app"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourceRoot}\engine\*"; DestDir: "{app}\engine"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourceRoot}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\第三方许可.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\AirMirror"; Filename: "{app}\app\{#MyAppExeName}"
Name: "{autodesktop}\AirMirror"; Filename: "{app}\app\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "附加选项："

[Run]
Filename: "{app}\app\{#MyAppExeName}"; Description: "{cm:LaunchProgram}"; Flags: nowait postinstall skipifsilent
