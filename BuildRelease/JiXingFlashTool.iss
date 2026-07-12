#ifndef MyAppName
  #define MyAppName "极星系统工具"
#endif

#ifndef MyAppEnName
  #define MyAppEnName "Polestar System Tool"
#endif

#ifndef MyAppProduct
  #define MyAppProduct MyAppName
#endif

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0.0"
#endif

#ifndef MyAppPublisher
  #define MyAppPublisher "中山市新星网络科技有限公司"
#endif

#ifndef MyAppCopyright
  #define MyAppCopyright "Copyright (C) 中山市新星网络科技有限公司"
#endif

#ifndef MyAppURL
  #define MyAppURL "https://github.com/ZSXinXing/JiXingFlashTool"
#endif

#ifndef MyAppExeName
  #define MyAppExeName "JiXingFlashTool.exe"
#endif

#ifndef MyOutputBaseFilename
  #define MyOutputBaseFilename "Polestar-System-Tool-Setup-" + MyAppVersion
#endif

#ifndef SourceDir
  #define SourceDir AddBackslash("..\JiXingFlashTool\bin\Release")
#endif

#ifndef IconFile
  #define IconFile "..\JiXingFlashTool\logo.ico"
#endif

#ifndef OutputDir
  #define OutputDir "..\release_out"
#endif

#define AppId "D89D95B4-6EEB-4D6A-91D1-6A3222FF9C12"

[Setup]
AppId={#AppId}
AppName={#MyAppProduct}
AppVersion={#MyAppVersion}
AppVerName={#MyAppProduct} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright={#MyAppCopyright}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename={#MyOutputBaseFilename}
SetupIconFile={#IconFile}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#MyAppProduct}
UninstallDisplayIcon={app}\{#MyAppExeName}
DefaultDirName={autopf}\{#MyAppEnName}

[Dirs]
Name: "{app}"; Permissions: users-full

[Languages]
Name: "ChineseSimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "English"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "db.sqlite3,app_config.json,app_config.json.bak,debug_log\*,Logs\*,log\*,tmp\*"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppProduct}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppProduct}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{group}\{cm:UninstallProgram,{#MyAppProduct}}"; Filename: "{uninstallexe}"; IconFilename: "{uninstallexe}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppProduct, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{cmd}"; Parameters: "/C """"{app}\Resources\Library\ADB\adb.exe"" kill-server >nul 2>&1"""; Flags: runhidden waituntilterminated skipifdoesntexist
Filename: "{cmd}"; Parameters: "/C ""taskkill /F /T /IM adb.exe >nul 2>&1"""; Flags: runhidden waituntilterminated

[Code]
var
  ShouldCloseAdbDuringInstall: Boolean;

procedure InitializeWizard;
begin
  ShouldCloseAdbDuringInstall := True;
end;

procedure CloseAdbForInstall;
var
  ResultCode: Integer;
  AdbPath: string;
begin
  AdbPath := ExpandConstant('{app}\Resources\Library\ADB\adb.exe');
  if FileExists(AdbPath) then
  begin
    Exec(
      ExpandConstant('{cmd}'),
      '/C ""' + AdbPath + '" kill-server >nul 2>&1"',
      '',
      SW_HIDE,
      ewWaitUntilTerminated,
      ResultCode);
  end;

  Exec(
    ExpandConstant('{cmd}'),
    '/C "taskkill /F /T /IM adb.exe >nul 2>&1"',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if CurPageID = wpPreparing then
  begin
    ShouldCloseAdbDuringInstall := True;
    if WizardForm.PreparingNoRadio.Visible and WizardForm.PreparingNoRadio.Checked then
    begin
      ShouldCloseAdbDuringInstall := False;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    if ShouldCloseAdbDuringInstall then
    begin
      CloseAdbForInstall;
    end;
  end;
end;
