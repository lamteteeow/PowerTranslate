; TEMPLATE: Inno Setup Script for Command Palette Extensions

#define ExtensionName "PowerTranslateExtension"
#define DisplayName "PowerTranslate"
#define DeveloperName "lamteteeow"
#define ExtensionClsid "fd4bd242-c3f8-47bb-89f0-5a6f7f14aecf"
#define AppVersion "1.1.4.0"


[Setup]
AppId={{{#ExtensionClsid}}}
AppName={#DisplayName}
AppVerName={#DisplayName}
AppVersion={#AppVersion}
AppPublisher={#DeveloperName}
SetupIconFile=Assets\PowerTranslateLogo.ico
UninstallDisplayIcon={app}\{#ExtensionName}.exe,0
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

[Code]
function ClsidKey: string;
begin
	Result := 'SOFTWARE\\Classes\\CLSID\\{' + '{#ExtensionClsid}' + '}';
end;

function ClsidKeyMalformed: string;
begin
	Result := 'SOFTWARE\\Classes\\CLSID\\{' + '{#ExtensionClsid}' + '}}';
end;

function ComServerCommand: string;
begin
	Result := '"' + ExpandConstant('{app}\\{#ExtensionName}.exe') + '" -RegisterProcessAsComServer';
end;

procedure RegisterComServer(RootKey: Integer);
begin
	RegWriteStringValue(RootKey, ClsidKey, '', '{#ExtensionName}');
	RegWriteStringValue(RootKey, ClsidKey + '\\LocalServer32', '', ComServerCommand);
end;

procedure CleanupMalformedKeys;
begin
	RegDeleteKeyIncludingSubkeys(HKLM, ClsidKeyMalformed);
	RegDeleteKeyIncludingSubkeys(HKCU, ClsidKeyMalformed);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
	if CurStep = ssPostInstall then
	begin
		CleanupMalformedKeys;
		RegisterComServer(HKLM);
		RegisterComServer(HKCU);
	end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
	if CurUninstallStep = usUninstall then
	begin
		RegDeleteKeyIncludingSubkeys(HKCU, ClsidKey);
		RegDeleteKeyIncludingSubkeys(HKLM, ClsidKey);
		CleanupMalformedKeys;
	end;
end;
