; TEMPLATE: Inno Setup Script for Command Palette Extensions

#define ExtensionName "PowerTranslateExtension"
#define DisplayName "PowerTranslate"
#define DeveloperName "lamteteeow"
#define ExtensionClsid "fd4bd242-c3f8-47bb-89f0-5a6f7f14aecf"
#define AppVersion "1.1.3.0"


[Setup]
AppId={{{#ExtensionClsid}}}
AppName={#DisplayName}
AppVersion={#AppVersion}
AppPublisher={#DeveloperName}
DefaultDirName={autopf}\{#ExtensionName}
OutputDir=bin\Release\installer
OutputBaseFilename={#ExtensionName}-Setup-{#AppVersion}-x64
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "bin\Release\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#DisplayName}"; Filename: "{app}\{#ExtensionName}.exe"

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Classes\CLSID\{{{#ExtensionClsid}}}"; ValueData: "{#ExtensionName}"
Root: HKCU; Subkey: "SOFTWARE\Classes\CLSID\{{{#ExtensionClsid}}}\LocalServer32"; ValueData: "{app}\{#ExtensionName}.exe -RegisterProcessAsComServer"
