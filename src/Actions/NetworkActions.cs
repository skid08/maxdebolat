namespace MaxDebloat;

internal static class NetworkActions
{
    public static IEnumerable<TweakAction> Get()
    {
        var a = new List<TweakAction>();

        a.Add(new TweakAction
        {
            Id = "net_hosts", Category = Category.Network,
            Name = "Block telemetry hosts (hosts file)", Desc = "Null-route Microsoft telemetry / phone-home domains.",
            Run = r =>
            {
                string[] hosts =
                {
                    "vortex.data.microsoft.com", "vortex-win.data.microsoft.com",
                    "telecommand.telemetry.microsoft.com", "telemetry.microsoft.com",
                    "watson.telemetry.microsoft.com", "watson.microsoft.com",
                    "settings-win.data.microsoft.com", "v10.events.data.microsoft.com",
                    "v20.events.data.microsoft.com", "self.events.data.microsoft.com",
                    "us.vortex-win.data.microsoft.com", "eu.vortex-win.data.microsoft.com",
                    "oca.telemetry.microsoft.com", "sqm.telemetry.microsoft.com",
                    "df.telemetry.microsoft.com", "reports.wes.df.telemetry.microsoft.com",
                    "services.wes.df.telemetry.microsoft.com", "diagnostics.support.microsoft.com",
                    "corp.sts.microsoft.com", "statsfe2.ws.microsoft.com", "statsfe1.ws.microsoft.com",
                    "feedback.windows.com", "feedback.microsoft-hohm.com", "feedback.search.microsoft.com",
                };
                var quoted = string.Join(",", Array.ConvertAll(hosts, h => "'0.0.0.0 " + h + "'"));
                r.Ps($@"
$hp = ""$env:SystemRoot\System32\drivers\etc\hosts""
$existing = Get-Content $hp -ErrorAction SilentlyContinue
$lines = @({quoted})
Add-Content -Path $hp -Value ""# MAX DEBLOAT telemetry block"" -ErrorAction SilentlyContinue
foreach($l in $lines){{ if($existing -notcontains $l){{ Add-Content -Path $hp -Value $l -ErrorAction SilentlyContinue }} }}
Write-Output ""hosts file updated""");
            }
        });

        a.Add(new TweakAction
        {
            Id = "net_llmnr", Category = Category.Network,
            Name = "Disable LLMNR", Desc = "Turn off Link-Local Multicast Name Resolution (leak/attack surface).",
            Run = r => r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DNSClient", "EnableMulticast", 0)
        });

        a.Add(new TweakAction
        {
            Id = "net_smb1", Category = Category.Network,
            Name = "Disable SMBv1", Desc = "Remove the legacy SMBv1 protocol.",
            Run = r =>
            {
                r.Ps("Set-SmbServerConfiguration -EnableSMB1Protocol $false -Force -ErrorAction SilentlyContinue");
                r.Shell("dism /online /norestart /disable-feature /featurename:SMB1Protocol");
            }
        });

        a.Add(new TweakAction
        {
            Id = "net_netbios", Category = Category.Network,
            Name = "Disable NetBIOS over TCP/IP", Desc = "Kill NetBIOS name broadcasts on all interfaces.",
            Run = r => r.Ps(@"
Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces' | ForEach-Object {
  Set-ItemProperty -Path $_.PSPath -Name NetbiosOptions -Value 2 -ErrorAction SilentlyContinue }")
        });

        a.Add(new TweakAction
        {
            Id = "net_throttle", Category = Category.Network,
            Name = "Disable network throttling", Desc = "NetworkThrottlingIndex=disabled for lower multiplayer latency.",
            Run = r => r.RegDword(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 0xffffffff)
        });

        a.Add(new TweakAction
        {
            Id = "net_nagle", Category = Category.Network,
            Name = "Disable Nagle's algorithm", Desc = "TcpAckFrequency=1 + TCPNoDelay=1 on every interface (lower latency).",
            Run = r => r.Ps(@"
Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces' | ForEach-Object {
  Set-ItemProperty -Path $_.PSPath -Name TcpAckFrequency -Value 1 -Type DWord -ErrorAction SilentlyContinue
  Set-ItemProperty -Path $_.PSPath -Name TCPNoDelay -Value 1 -Type DWord -ErrorAction SilentlyContinue }")
        });

        a.Add(new TweakAction
        {
            Id = "net_ipv6", Category = Category.Network,
            Name = "Disable IPv6", Desc = "Disable IPv6 stack (OFF by default; some games/networks need it).", InNuke = false,
            Run = r => r.RegDword(@"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "DisabledComponents", 0xff)
        });

        return a;
    }
}
