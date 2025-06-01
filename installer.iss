#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

#define MyAppVersion AppVersion

[Setup]
AppName=WDS Super Menu
AppVersion={#MyAppVersion}
DefaultDirName={pf}\WDSSuperMenu
OutputDir=output
OutputBaseFilename=WDSSuperMenu-{#MyAppVersion}-Installer
Compression=lzma
SolidCompression=yes
SetupIconFile=installer.ico


[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
; Start Menu shortcut
Name: "{group}\WDS Super Menu"; Filename: "{app}\WDSSuperMenu.exe"

; Desktop shortcut
Name: "{commondesktop}\WDS Super Menu"; Filename: "{app}\WDSSuperMenu.exe"

