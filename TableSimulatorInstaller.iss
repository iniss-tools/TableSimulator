; Instalator simulatora tabul. Spolocny zaklad je v ToolsCore\Installer (Common.iss, DotNet.iss).
; Pred kompilaciou treba publish (len win-x64, do prazdneho priecinka): dotnet publish TableSimulator\TableSimulator.csproj -c Release
;
; Predvolene sa instaluje pre vsetkych pouzivatelov ako sluzba Windows s nastaveniami v %ProgramData%\TableSimulator
; (TableSimulator.exe --install-service) a s pravidlom Windows Firewall pre program. Bez sluzby (aj pri instalacii
; len pre pouzivatela) bezi server v okne s nastaveniami v %AppData%\TableSimulator. Udaje pri odinstalovani
; ostavaju, ak ich pouzivatel nezmaze.

#define MyAppName "TableSimulator"
#define MyAppExeName "TableSimulator.exe"
#define MyAppDir SourcePath + "TableSimulator\bin\Release\net10.0\win-x64\publish"
#define MyOutputBaseFilename "tablesimulator-setup"
; sluzba a pravidlo firewallu potrebuju prava spravcu - instalacia len pre pouzivatela sa da zvolit v dialogu
#define MyPrivilegesRequired "admin"
; WindowsService.Name v serveri
#define ServiceName "TableSimulator"
; WindowsService.NotStartedExitCode - sluzba je nainstalovana, ale server sa nespustil
#define ServiceNotStartedCode 4

#include "..\ToolsCore\Installer\Common.iss"

[CustomMessages]
english.ServerGroup=Server:
english.ServiceTask=Run as a Windows service (starts with Windows, also without a signed-in user)
english.FirewallTask=Allow access from the network in Windows Firewall (domain and private networks)
english.ServiceInstalling=Installing the Windows service...
english.ServiceFailed=The Windows service was not installed:%n%n%1
english.ServiceNotStarted=The Windows service is installed, but the simulator did not start:%n%n%1
english.OpenInBrowser=Open %1 in the browser
english.DeleteData=Delete the simulator data as well?%n%n%1%n%nThe folder contains the database, recordings, panel media, certificates and logs.
czech.ServerGroup=Server:
czech.ServiceTask=Spouštět jako službu Windows (startuje se systémem, i bez přihlášeného uživatele)
czech.FirewallTask=Povolit přístup ze sítě ve Windows Firewall (doménová a soukromá síť)
czech.ServiceInstalling=Instaluje se služba Windows...
czech.ServiceFailed=Služba Windows se nenainstalovala:%n%n%1
czech.ServiceNotStarted=Služba Windows je nainstalovaná, ale simulátor se nespustil:%n%n%1
czech.OpenInBrowser=Otevřít %1 v prohlížeči
czech.DeleteData=Smazat i data simulátoru?%n%n%1%n%nSložka obsahuje databázi, záznamy, média panelů, certifikáty a logy.
slovak.ServerGroup=Server:
slovak.ServiceTask=Spúšťať ako službu Windows (štartuje so systémom, aj bez prihláseného používateľa)
slovak.FirewallTask=Povoliť prístup zo siete vo Windows Firewall (doménová a súkromná sieť)
slovak.ServiceInstalling=Inštaluje sa služba Windows...
slovak.ServiceFailed=Služba Windows sa nenainštalovala:%n%n%1
slovak.ServiceNotStarted=Služba Windows je nainštalovaná, ale simulátor sa nespustil:%n%n%1
slovak.OpenInBrowser=Otvoriť %1 v prehliadači
slovak.DeleteData=Zmazať aj údaje simulátora?%n%n%1%n%nPriečinok obsahuje databázu, záznamy, médiá panelov, certifikáty a logy.

[Tasks]
; len pri instalacii pre vsetkych pouzivatelov (prava spravcu)
Name: "service"; Description: "{cm:ServiceTask}"; GroupDescription: "{cm:ServerGroup}"; Check: IsAdminInstallMode
Name: "firewall"; Description: "{cm:FirewallTask}"; GroupDescription: "{cm:ServerGroup}"; Check: IsAdminInstallMode
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; publish je vzdy cisty (TableSimulator.csproj ho pred zostavenim zmaze) - bez vynimiek a bez prazdnych priecinkov
Source: "{#MyAppDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
; Pri sluzbe ide odkaz na jej priecinok nastaveni - program vtedy server nespusti, len otvori bieziaci v prehliadaci.
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "{code:ServerParams}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "{code:ServerParams}"; Tasks: desktopicon

[Run]
; spustenie (pri sluzbe otvorenie v prehliadaci) ponukne posledna stranka sprievodcu; pri tichej instalacii nie
Filename: "{app}\{#MyAppExeName}"; Parameters: "{code:ServerParams}"; Description: "{code:LaunchDescription}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Pred zmazanim suborov zastavit a odstranit sluzbu a zmazat pravidlo firewallu - ked nie su, prikaz skonci chybou,
; nevadi. Bez programu (sc.exe), aby odinstalovanie fungovalo aj bez .NET.
Filename: "{sys}\net.exe"; Parameters: "stop {#ServiceName}"; Flags: runhidden waituntilterminated; RunOnceId: "StopService"; Check: IsAdminInstallMode
Filename: "{sys}\sc.exe"; Parameters: "delete {#ServiceName}"; Flags: runhidden waituntilterminated; RunOnceId: "DeleteService"; Check: IsAdminInstallMode
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""{#MyAppName}"""; Flags: runhidden waituntilterminated; RunOnceId: "Firewall"; Check: IsAdminInstallMode

; server ASP.NET Core bez WinForms
#define NeedRuntime
#define NeedAspNetCore
#include "..\ToolsCore\Installer\DotNet.iss"

[Code]
const
  ServiceKey = 'SYSTEM\CurrentControlSet\Services\{#ServiceName}';

// Parametre programu: pri sluzbe jej priecinok nastaveni (bez --data by program pouzil %AppData% pouzivatela).
function ServerParams(Param: String): String;
begin
  if WizardIsTaskSelected('service') then
    Result := '--data "' + ExpandConstant('{commonappdata}\{#MyAppName}') + '"'
  else
    Result := '';
end;

function LaunchDescription(Param: String): String;
begin
  if WizardIsTaskSelected('service') then
    Result := FmtMessage(CustomMessage('OpenInBrowser'), ['{#MyAppName}'])
  else
    Result := FmtMessage(CustomMessage('LaunchProgram'), ['{#MyAppName}']);
end;

function ServiceInstalled: Boolean;
begin
  Result := RegKeyExists(HKLM, ServiceKey);
end;

// Bieziaca sluzba drzi subory programu - pred kopirovanim ju zastavit (spusti ju znova --install-service).
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Result := '';
  if IsAdminInstallMode and ServiceInstalled then
    // net stop pocka, kym sa sluzba zastavi; ked nebezi, skonci chybou - nevadi
    Exec(ExpandConstant('{sys}\net.exe'), 'stop {#ServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure UpdateFirewall;
var
  Netsh: String;
  ResultCode: Integer;
begin
  Netsh := ExpandConstant('{sys}\netsh.exe');
  // pravidlo predchadzajucej instalacie (mohla byt v inom priecinku); ked nie je, netsh skonci chybou - nevadi
  Exec(Netsh, 'advfirewall firewall delete rule name="{#MyAppName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if not WizardIsTaskSelected('firewall') then
    Exit;
  // pravidlo pre program pokryje web aj linky k INISSu na lubovolnom porte
  Exec(Netsh, 'advfirewall firewall add rule name="{#MyAppName}" dir=in action=allow enable=yes profile=domain,private program="' +
    ExpandConstant('{app}\{#MyAppExeName}') + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Log('netsh add rule: ' + IntToStr(ResultCode));
end;

function OutputText(const Output: TExecOutput): String;
var
  I: Integer;
begin
  Result := '';
  for I := 0 to GetArrayLength(Output.StdOut) - 1 do
    Result := Result + Output.StdOut[I] + #13#10;
  for I := 0 to GetArrayLength(Output.StdErr) - 1 do
    Result := Result + Output.StdErr[I] + #13#10;
end;

// --install-service sluzbu zalozi alebo zmeni (iny priecinok programu), spusti a pocka na server (pri zlyhani vypise dovod),
// --uninstall-service ju odstrani.
procedure RunServer(const Params: String; const ReportError: Boolean);
var
  ResultCode: Integer;
  Output: TExecOutput;
  Text: String;
begin
  try
    if ExecAndCaptureOutput(ExpandConstant('{app}\{#MyAppExeName}'), Params, ExpandConstant('{app}'), SW_SHOWNORMAL,
      ewWaitUntilTerminated, ResultCode, Output) then
      Text := OutputText(Output)
    else
      Text := SysErrorMessage(ResultCode);
  except
    ResultCode := -1;
    Text := GetExceptionMessage;
  end;

  Log(Params + ': ' + IntToStr(ResultCode) + #13#10 + Text);
  if ReportError and (ResultCode = {#ServiceNotStartedCode}) then
    SuppressibleMsgBox(FmtMessage(CustomMessage('ServiceNotStarted'), [Trim(Text)]), mbError, MB_OK, IDOK)
  else if ReportError and (ResultCode <> 0) then
    SuppressibleMsgBox(FmtMessage(CustomMessage('ServiceFailed'), [Trim(Text)]), mbError, MB_OK, IDOK);
end;

// Hlavna implementacia sa vola az po DotNetCurStepChanged - chybajuci .NET je uz nainstalovany a program sa spusti.
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep <> ssPostInstall) or not IsAdminInstallMode then
    Exit;
  UpdateFirewall;
  if WizardIsTaskSelected('service') then
  begin
    WizardForm.StatusLabel.Caption := CustomMessage('ServiceInstalling');
    RunServer('--install-service ' + ServerParams(''), True);
  end
  else if ServiceInstalled then
    // prechod zo sluzby na program v okne
    RunServer('--uninstall-service', False);
end;

procedure AskDeleteData(const Folder: String);
begin
  if DirExists(Folder) and (SuppressibleMsgBox(FmtMessage(CustomMessage('DeleteData'), [Folder]), mbConfirmation,
    MB_YESNO or MB_DEFBUTTON2, IDNO) = IDYES) then
    DelTree(Folder, True, True, True);
end;

// Udaje simulatora (pouzivatelia, linky, zaznamy) su cenne - predvolene ostavaju, pri tichom odinstalovani vzdy.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep <> usPostUninstall then
    Exit;
  if IsAdminInstallMode then
    AskDeleteData(ExpandConstant('{commonappdata}\{#MyAppName}'));
  AskDeleteData(ExpandConstant('{userappdata}\{#MyAppName}'));
end;
