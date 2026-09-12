namespace MaxDebloat;

internal static class StartupActions
{
    public static IEnumerable<TweakAction> Get()
    {
        var a = new List<TweakAction>();

        a.Add(new TweakAction
        {
            Id = "start_common", Category = Category.Startup,
            Name = "Purge common startup entries", Desc = "Remove OneDrive, Teams, Edge, Spotify web helper, etc. from Run keys.",
            Run = r =>
            {
                foreach (var name in new[] { "OneDrive", "OneDriveSetup", "com.squirrel.Teams.Teams",
                    "MicrosoftEdgeAutoLaunch", "Microsoft Edge Update", "Spotify", "SpotifyWeb", "Discord" })
                {
                    r.RegDelVal(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Run", name);
                    r.RegDelVal(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run", name);
                }
            }
        });

        a.Add(new TweakAction
        {
            Id = "start_edge_boost", Category = Category.Startup,
            Name = "Disable Edge startup boost", Desc = "Stop Edge preloading at logon.",
            Run = r =>
            {
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Edge", "StartupBoostEnabled", 0);
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Edge", "BackgroundModeEnabled", 0);
            }
        });

        a.Add(new TweakAction
        {
            Id = "start_delay", Category = Category.Startup,
            Name = "Remove startup delay", Desc = "Zero the artificial Explorer startup delay timer.",
            Run = r => r.RegDword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize", "StartupDelayInMSec", 0)
        });

        a.Add(new TweakAction
        {
            Id = "start_bg_apps", Category = Category.Startup,
            Name = "Disable background apps", Desc = "Stop UWP apps running in the background at logon.",
            Run = r =>
            {
                r.RegDword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1);
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", 2);
            }
        });

        a.Add(new TweakAction
        {
            Id = "start_lockscreen_tips", Category = Category.Startup,
            Name = "Disable lock-screen tips & ads", Desc = "No Spotlight tips or promoted content on the lock screen.",
            Run = r =>
            {
                string cd = @"HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
                r.RegDword(cd, "RotatingLockScreenOverlayEnabled", 0);
                r.RegDword(cd, "SubscribedContent-338387Enabled", 0);
            }
        });

        return a;
    }
}
