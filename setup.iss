[Setup]
AppName=DesktopWallpapers
AppVersion={#AppVersion}
DefaultDirName={autopf}\DesktopWallpapers
DefaultGroupName=DesktopWallpapers
OutputDir=installer_output
OutputBaseFilename=DesktopWallpapers_Setup
Compression=lzma
SolidCompression=yes

[Files]
; Inkluderar alla publicerade filer från .NET-bygget
Source: "DesktopWallpapers\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\DesktopWallpapers"; Filename: "{app}\DesktopWallpapers.exe"
Name: "{autodesktop}\DesktopWallpapers"; Filename: "{app}\DesktopWallpapers.exe"
