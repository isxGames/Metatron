[Setup]
AppName=Metatron
AppVersion=0.0.6
DefaultDirName={reg:HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\InnerSpace.exe,Path}\.NET Programs
OutputBaseFilename=MetatronSetup

[Files]
Source: "build\Metatron.exe"; DestDir: "{app}"; Check: CheckRegistryKeyExists
Source: "build\isxGamesPatcher.exe"; DestDir: "{app}"

[Code]
function CheckRegistryKeyExists: Boolean;
var
  Value: String;
begin
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\InnerSpace.exe', 'Path', Value) then
  begin
    MsgBox('Innerspace not detected. Installation cannot proceed. Please make sure Innerspace is installed and has been run at least once to complete setup.', mbError, MB_OK);
    Result := False;
  end
  else
    Result := True;
end;
