namespace MaxDebloat;

/// <summary>
/// Maximum-aggression Windows Defender neutralization. NOTE: on a current build with Tamper
/// Protection still ON, some of these writes are reverted by Defender itself. For guaranteed
/// full removal, turn Tamper Protection OFF first (Settings &gt; Privacy &amp; security &gt; Windows
/// Security &gt; Virus &amp; threat protection &gt; Manage settings) or run from an offline/WinPE
/// context. This code takes ownership of Defender's protected registry keys and does the most
/// that is achievable from a live elevated session.
/// </summary>
internal static class DefenderActions
{
    // PowerShell that enables SeTakeOwnershipPrivilege, seizes a HKLM registry key for Admins,
    // then sets its "Start" value to 4 (disabled). Used to force Defender service keys off.
    private const string TakeownAndDisableSvc = @"
$signature = @'
using System;
using System.Runtime.InteropServices;
public static class Priv {
  [DllImport(""advapi32.dll"", SetLastError=true)] public static extern bool OpenProcessToken(IntPtr h,int acc,out IntPtr tok);
  [DllImport(""kernel32.dll"")] public static extern IntPtr GetCurrentProcess();
  [DllImport(""advapi32.dll"", SetLastError=true)] public static extern bool LookupPrivilegeValue(string s,string n,out long l);
  [StructLayout(LayoutKind.Sequential)] public struct TP { public int Count; public long Luid; public int Attr; }
  [DllImport(""advapi32.dll"", SetLastError=true)] public static extern bool AdjustTokenPrivileges(IntPtr t,bool d,ref TP p,int l,IntPtr pr,IntPtr rl);
  public static void Enable(string name){ IntPtr tok; OpenProcessToken(GetCurrentProcess(),0x28,out tok); long luid; LookupPrivilegeValue(null,name,out luid); TP tp=new TP(); tp.Count=1; tp.Luid=luid; tp.Attr=2; AdjustTokenPrivileges(tok,false,ref tp,0,IntPtr.Zero,IntPtr.Zero); }
}
'@
Add-Type -TypeDefinition $signature -ErrorAction SilentlyContinue
[Priv]::Enable('SeTakeOwnershipPrivilege')
[Priv]::Enable('SeRestorePrivilege')
function Force-DisableSvc($svc){
  $path = ""SYSTEM\CurrentControlSet\Services\$svc""
  try {
    $key = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($path, [Microsoft.Win32.RegistryKeyPermissionCheck]::ReadWriteSubTree, [System.Security.AccessControl.RegistryRights]::TakeOwnership)
    if($key -eq $null){ return }
    $admins = New-Object System.Security.Principal.SecurityIdentifier('S-1-5-32-544')
    $acl = $key.GetAccessControl()
    $acl.SetOwner($admins)
    $key.SetAccessControl($acl)
    $rule = New-Object System.Security.AccessControl.RegistryAccessRule($admins,'FullControl','Allow')
    $acl.SetAccessRule($rule)
    $key.SetAccessControl($acl)
    $key.SetValue('Start',4,[Microsoft.Win32.RegistryValueKind]::DWord)
    $key.Close()
    Write-Output ""forced $svc Start=4""
  } catch { Write-Output ""could not seize $svc : $($_.Exception.Message)"" }
}
foreach($s in @('WinDefend','WdNisSvc','Sense','WdNisDrv','WdFilter','WdBoot','webthreatdefsvc','webthreatdefusersvc')){ Force-DisableSvc $s }
";

    public static IEnumerable<TweakAction> Get()
    {
        var a = new List<TweakAction>();

        a.Add(new TweakAction
        {
            Id = "def_tamper", Category = Category.Defender,
            Name = "Attempt Tamper Protection off", Desc = "Best-effort clear of Tamper Protection (may need manual toggle).",
            Run = r =>
            {
                string feat = @"HKLM\SOFTWARE\Microsoft\Windows Defender\Features";
                r.RegDword(feat, "TamperProtection", 0);
                r.RegDword(feat, "TamperProtectionSource", 2);
                r.Ps("Set-MpPreference -DisableTamperProtection $true -ErrorAction SilentlyContinue");
                r.Log("  Note: if this reverts, disable Tamper Protection manually, then re-run.", LogLevel.Warn);
            }
        });

        a.Add(new TweakAction
        {
            Id = "def_realtime", Category = Category.Defender,
            Name = "Disable real-time protection", Desc = "Turn off RTP, behavior monitoring, IOAV, script scanning.",
            Run = r =>
            {
                string pol = @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender";
                r.RegDword(pol, "DisableAntiSpyware", 1);
                r.RegDword(pol, "DisableAntiVirus", 1);
                string rtp = pol + @"\Real-Time Protection";
                r.RegDword(rtp, "DisableRealtimeMonitoring", 1);
                r.RegDword(rtp, "DisableBehaviorMonitoring", 1);
                r.RegDword(rtp, "DisableOnAccessProtection", 1);
                r.RegDword(rtp, "DisableScanOnRealtimeEnable", 1);
                r.RegDword(rtp, "DisableIOAVProtection", 1);
                r.Ps(@"
Set-MpPreference -DisableRealtimeMonitoring $true -ErrorAction SilentlyContinue
Set-MpPreference -DisableBehaviorMonitoring $true -ErrorAction SilentlyContinue
Set-MpPreference -DisableScriptScanning $true -ErrorAction SilentlyContinue
Set-MpPreference -DisableIOAVProtection $true -ErrorAction SilentlyContinue
Set-MpPreference -MAPSReporting 0 -ErrorAction SilentlyContinue
Set-MpPreference -SubmitSamplesConsent 2 -ErrorAction SilentlyContinue
Set-MpPreference -DisableArchiveScanning $true -ErrorAction SilentlyContinue");
            }
        });

        a.Add(new TweakAction
        {
            Id = "def_cloud", Category = Category.Defender,
            Name = "Disable cloud & sample submission", Desc = "No MAPS cloud reporting, no automatic sample upload.",
            Run = r =>
            {
                string spynet = @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet";
                r.RegDword(spynet, "SpyNetReporting", 0);
                r.RegDword(spynet, "SubmitSamplesConsent", 2);
                r.RegDword(spynet, "DisableBlockAtFirstSeen", 1);
            }
        });

        a.Add(new TweakAction
        {
            Id = "def_services", Category = Category.Defender,
            Name = "Kill Defender services (take ownership)", Desc = "Seize protected keys and set WinDefend/Sense/etc. to disabled.",
            Run = r =>
            {
                r.Ps(TakeownAndDisableSvc);
                r.Shell("sc stop WinDefend");
                r.Shell("sc stop WdNisSvc");
                r.Shell("sc stop Sense");
                r.Shell("sc stop webthreatdefsvc");
            }
        });

        a.Add(new TweakAction
        {
            Id = "def_tasks", Category = Category.Defender,
            Name = "Disable Defender scheduled tasks", Desc = "Cache maintenance, cleanup, scans, verification.",
            Run = r =>
            {
                foreach (var t in new[]
                {
                    @"\Microsoft\Windows\Windows Defender\Windows Defender Cache Maintenance",
                    @"\Microsoft\Windows\Windows Defender\Windows Defender Cleanup",
                    @"\Microsoft\Windows\Windows Defender\Windows Defender Scheduled Scan",
                    @"\Microsoft\Windows\Windows Defender\Windows Defender Verification",
                })
                    r.DisableTask(t);
            }
        });

        a.Add(new TweakAction
        {
            Id = "def_smartscreen", Category = Category.Defender,
            Name = "Disable SmartScreen", Desc = "Turn off SmartScreen for shell, Edge, and Store apps.",
            Run = r =>
            {
                r.RegSz(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", "Off");
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "EnableSmartScreen", 0);
                r.RegDword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", 0);
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Edge", "SmartScreenEnabled", 0);
            }
        });

        a.Add(new TweakAction
        {
            Id = "def_securityhealth", Category = Category.Defender,
            Name = "Kill Security Health tray & service", Desc = "Remove the Security notification icon and disable wscsvc/SecurityHealthService.",
            Run = r =>
            {
                r.Shell("taskkill /f /im SecurityHealthSystray.exe");
                r.RegDelVal(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "SecurityHealth");
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Notifications", "DisableNotifications", 1);
                r.DisableService("SecurityHealthService");
                r.DisableService("wscsvc");
                r.DisableService("Sense");
            }
        });

        a.Add(new TweakAction
        {
            Id = "def_contextmenu", Category = Category.Defender,
            Name = "Remove 'Scan with Defender'", Desc = "Strip the Defender scan entry from the right-click menu.",
            Run = r =>
            {
                r.RegDelKey(@"HKCR\*\shellex\ContextMenuHandlers\EPP");
                r.RegDelKey(@"HKCR\Directory\shellex\ContextMenuHandlers\EPP");
                r.RegDelKey(@"HKCR\Drive\shellex\ContextMenuHandlers\EPP");
            }
        });

        return a;
    }
}
