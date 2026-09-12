namespace MaxDebloat;

internal static class GamingActions
{
    public static IEnumerable<TweakAction> Get()
    {
        var a = new List<TweakAction>();

        a.Add(new TweakAction
        {
            Id = "game_powerplan", Category = Category.Gaming,
            Name = "Ultimate Performance power plan", Desc = "Duplicate + activate the Ultimate Performance scheme; disable sleep.",
            Run = r =>
            {
                r.Shell("powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
                r.Shell("powercfg -setactive e9a42b02-d5df-448d-aa00-03f14749eb61");
                r.Shell("powercfg -change -standby-timeout-ac 0");
                r.Shell("powercfg -change -monitor-timeout-ac 0");
                r.Shell("powercfg -change -disk-timeout-ac 0");
                r.Shell("powercfg -h off");
            }
        });

        a.Add(new TweakAction
        {
            Id = "game_throttle", Category = Category.Gaming,
            Name = "Disable CPU power throttling", Desc = "Stop Windows throttling foreground apps.",
            Run = r => r.RegDword(@"HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1)
        });

        a.Add(new TweakAction
        {
            Id = "game_gamedvr", Category = Category.Gaming,
            Name = "Disable Game DVR / recording", Desc = "Turn off background recording overhead (keeps FPS steady).",
            Run = r =>
            {
                r.RegDword(@"HKCU\System\GameConfigStore", "GameDVR_Enabled", 0);
                r.RegDword(@"HKCU\System\GameConfigStore", "GameDVR_FSEBehaviorMode", 2);
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0);
                r.RegDword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0);
            }
        });

        a.Add(new TweakAction
        {
            Id = "game_gamemode", Category = Category.Gaming,
            Name = "Enable Game Mode", Desc = "Prioritize the active game for CPU/GPU scheduling.",
            Run = r =>
            {
                r.RegDword(@"HKCU\Software\Microsoft\GameBar", "AutoGameModeEnabled", 1);
                r.RegDword(@"HKCU\Software\Microsoft\GameBar", "AllowAutoGameMode", 1);
            }
        });

        a.Add(new TweakAction
        {
            Id = "game_hags", Category = Category.Gaming,
            Name = "Hardware-accelerated GPU scheduling", Desc = "Enable HAGS (HwSchMode=2). Needs a reboot + supported GPU.",
            Run = r => r.RegDword(@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2)
        });

        a.Add(new TweakAction
        {
            Id = "game_mmcss", Category = Category.Gaming,
            Name = "Tune MMCSS for games", Desc = "Max GPU/CPU priority for the Games multimedia profile.",
            Run = r =>
            {
                string sys = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
                r.RegDword(sys, "SystemResponsiveness", 0);
                r.RegDword(sys, "NetworkThrottlingIndex", 0xffffffff);
                string games = sys + @"\Tasks\Games";
                r.RegSz(games, "GPU Priority", "8");
                r.RegSz(games, "Priority", "6");
                r.RegSz(games, "Scheduling Category", "High");
                r.RegSz(games, "SFIO Priority", "High");
            }
        });

        a.Add(new TweakAction
        {
            Id = "game_priority", Category = Category.Gaming,
            Name = "Foreground CPU priority boost", Desc = "Win32PrioritySeparation=26 to favor the active window.",
            Run = r => r.RegDword(@"HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 0x26)
        });

        a.Add(new TweakAction
        {
            Id = "game_mouse", Category = Category.Gaming,
            Name = "Disable mouse acceleration", Desc = "Raw 1:1 mouse input (Enhance pointer precision off).",
            Run = r =>
            {
                r.RegSz(@"HKCU\Control Panel\Mouse", "MouseSpeed", "0");
                r.RegSz(@"HKCU\Control Panel\Mouse", "MouseThreshold1", "0");
                r.RegSz(@"HKCU\Control Panel\Mouse", "MouseThreshold2", "0");
            }
        });

        a.Add(new TweakAction
        {
            Id = "game_fso", Category = Category.Gaming,
            Name = "Disable fullscreen optimizations", Desc = "Global FSO off for lower latency in exclusive fullscreen.",
            Run = r =>
            {
                string gcs = @"HKCU\System\GameConfigStore";
                r.RegDword(gcs, "GameDVR_DXGIHonorFSEWindowsCompatible", 1);
                r.RegDword(gcs, "GameDVR_HonorUserFSEBehaviorMode", 1);
                r.RegDword(gcs, "GameDVR_EFSEFeatureFlags", 0);
            }
        });

        a.Add(new TweakAction
        {
            Id = "game_visual", Category = Category.Gaming,
            Name = "Visual effects: performance", Desc = "Kill animations/shadows/transparency for snappier UI.",
            Run = r =>
            {
                r.RegDword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2);
                r.RegDword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0);
                r.RegSz(@"HKCU\Control Panel\Desktop", "MenuShowDelay", "0");
                r.Reg(@"HKCU\Control Panel\Desktop", "UserPreferencesMask", "REG_BINARY", "9012038010000000");
            }
        });

        a.Add(new TweakAction
        {
            Id = "game_memcomp", Category = Category.Gaming,
            Name = "Disable memory compression", Desc = "Turn off MMAgent memory compression (frees CPU on big-RAM rigs).",
            Run = r => r.Ps("Disable-MMAgent -MemoryCompression -ErrorAction SilentlyContinue")
        });

        return a;
    }
}
