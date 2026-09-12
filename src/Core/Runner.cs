using System.Diagnostics;
using System.Security.Principal;
using System.Text;

namespace MaxDebloat;

internal enum LogLevel { Info, Ok, Warn, Err, Cmd }

/// <summary>
/// Executes external processes (cmd, powershell, reg, sc, schtasks, dism, ...) and streams
/// their output to a log sink. Every helper swallows failures loudly (logs them) instead of
/// throwing, because this is fire-and-forget one-way debloat.
/// </summary>
internal sealed class Runner
{
    public Action<string, LogLevel>? Sink;

    public void Log(string msg, LogLevel lvl = LogLevel.Info) => Sink?.Invoke(msg, lvl);

    public static bool IsAdmin()
    {
        try
        {
            using var id = WindowsIdentity.GetCurrent();
            var p = new WindowsPrincipal(id);
            return p.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    /// <summary>Run an executable, stream stdout/stderr to the log, return exit code (-1 on failure).</summary>
    public int Exec(string exe, string args, int timeoutMs = 180000, bool quiet = false)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            using var p = new Process { StartInfo = psi };

            p.OutputDataReceived += (_, e) =>
            {
                if (!quiet && !string.IsNullOrWhiteSpace(e.Data))
                    Log("  " + e.Data.TrimEnd(), LogLevel.Info);
            };
            p.ErrorDataReceived += (_, e) =>
            {
                if (!quiet && !string.IsNullOrWhiteSpace(e.Data))
                    Log("  " + e.Data.TrimEnd(), LogLevel.Warn);
            };

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            if (!p.WaitForExit(timeoutMs))
            {
                try { p.Kill(true); } catch { }
                Log($"  ! timed out: {exe} {args}", LogLevel.Err);
                return -1;
            }
            p.WaitForExit(); // flush async buffers
            return p.ExitCode;
        }
        catch (Exception ex)
        {
            Log($"  ! failed to run {exe}: {ex.Message}", LogLevel.Err);
            return -1;
        }
    }

    /// <summary>Run a process and return its stdout (for snapshots). Never throws.</summary>
    public string Capture(string exe, string args, int timeoutMs = 30000)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
            };
            using var p = Process.Start(psi);
            if (p == null) return "";
            string outp = p.StandardOutput.ReadToEnd();
            if (!p.WaitForExit(timeoutMs)) { try { p.Kill(true); } catch { } }
            return outp;
        }
        catch { return ""; }
    }

    /// <summary>Run a cmd.exe command line.</summary>
    public int Shell(string cmdLine) => Exec("cmd.exe", "/d /c " + cmdLine);

    /// <summary>Run a PowerShell script block via -EncodedCommand (no quoting hell).</summary>
    public int Ps(string script)
    {
        var wrapped = "$ErrorActionPreference='SilentlyContinue';$ProgressPreference='SilentlyContinue';" + script;
        var enc = Convert.ToBase64String(Encoding.Unicode.GetBytes(wrapped));
        return Exec("powershell.exe",
            "-NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand " + enc);
    }

    // ---- Registry helpers (reg.exe is used so protected/policy hives behave predictably) ----

    public void Reg(string keyPath, string name, string type, string data)
    {
        string valPart = name == "" ? "/ve" : $"/v \"{name}\"";
        Shell($"reg add \"{keyPath}\" {valPart} /t {type} /d \"{data}\" /f");
    }

    public void RegDword(string keyPath, string name, long value) => Reg(keyPath, name, "REG_DWORD", value.ToString());
    public void RegSz(string keyPath, string name, string value)  => Reg(keyPath, name, "REG_SZ", value);

    public void RegDelKey(string keyPath) => Shell($"reg delete \"{keyPath}\" /f");
    public void RegDelVal(string keyPath, string name) => Shell($"reg delete \"{keyPath}\" /v \"{name}\" /f");

    // ---- Services ----

    /// <summary>Stop a service and set it to disabled, forcing the registry Start value as backup.</summary>
    public void DisableService(string name)
    {
        Log($"[svc] disabling {name}", LogLevel.Cmd);
        Shell($"sc stop \"{name}\"");
        Shell($"sc config \"{name}\" start= disabled");
        // Registry backstop for services sc.exe cannot reconfigure at runtime:
        RegDword($"HKLM\\SYSTEM\\CurrentControlSet\\Services\\{name}", "Start", 4);
    }

    // ---- AppX packages ----

    public void RemoveAppx(string pattern)
    {
        Log($"[app] removing *{pattern}*", LogLevel.Cmd);
        Ps($@"
Get-AppxPackage -AllUsers ""*{pattern}*"" | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue
Get-AppxPackage ""*{pattern}*"" | Remove-AppxPackage -ErrorAction SilentlyContinue
Get-AppxProvisionedPackage -Online | Where-Object {{ $_.PackageName -like ""*{pattern}*"" }} | ForEach-Object {{ Remove-AppxProvisionedPackage -Online -PackageName $_.PackageName -AllUsers -ErrorAction SilentlyContinue }}
");
    }

    // ---- Scheduled tasks ----

    public void DisableTask(string taskPath)
    {
        Log($"[task] disabling {taskPath}", LogLevel.Cmd);
        Shell($"schtasks /Change /TN \"{taskPath}\" /Disable");
    }
}
