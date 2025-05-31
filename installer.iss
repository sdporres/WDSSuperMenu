[Setup]
AppName=WDSSuperMenu
AppVersion=1.0
DefaultDirName={pf}\WDSSuperMenu
OutputDir=output
OutputBaseFilename=WDSSuperMenuInstaller
Compression=lzma
SolidCompression=yes

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\WDSSuperMenu"; Filename: "{app}\WDSSuperMenu.exe"
