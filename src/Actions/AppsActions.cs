namespace MaxDebloat;

internal static class AppsActions
{
    public static IEnumerable<TweakAction> Get()
    {
        var a = new List<TweakAction>();

        // --- AI / Copilot / Widgets: highest priority strip ---
        a.Add(ActionRegistry.App("Copilot", "Copilot", "Remove Windows Copilot AI assistant package."));
        a.Add(new TweakAction
        {
            Id = "app_copilot_policy", Category = Category.Apps,
            Name = "Copilot (policy lockout)", Desc = "Registry policy to keep Copilot from ever coming back.",
            Run = r =>
            {
                r.RegDword(@"HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1);
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1);
                r.RegDword(@"HKCU\Software\Microsoft\Windows\Shell\Copilot\BingChat", "IsUserEligible", 0);
                r.RegDword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", 0);
            }
        });
        a.Add(ActionRegistry.App("Widgets (Web Experience)", "WebExperience", "Remove the Widgets / news feed panel."));
        a.Add(new TweakAction
        {
            Id = "app_widgets_policy", Category = Category.Apps,
            Name = "Widgets (taskbar off)", Desc = "Hide the Widgets taskbar button and disable the feed.",
            Run = r =>
            {
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0);
                r.RegDword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", 0);
            }
        });

        // --- OneDrive: full removal ---
        a.Add(new TweakAction
        {
            Id = "app_onedrive", Category = Category.Apps,
            Name = "OneDrive (uninstall)", Desc = "Kill OneDrive, run its uninstaller, purge from Explorer + startup.",
            Run = r =>
            {
                r.Shell("taskkill /f /im OneDrive.exe");
                r.Shell(@"%SystemRoot%\System32\OneDriveSetup.exe /uninstall");
                r.Shell(@"%SystemRoot%\SysWOW64\OneDriveSetup.exe /uninstall");
                r.RegDelVal(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Run", "OneDrive");
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\OneDrive", "DisableFileSyncNGSC", 1);
                r.Ps(@"Remove-Item -Path 'HKCR:\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}' -Recurse -ErrorAction SilentlyContinue");
            }
        });

        // --- Edge background processes ---
        a.Add(new TweakAction
        {
            Id = "app_edge_background", Category = Category.Apps,
            Name = "Edge background/telemetry", Desc = "Stop Edge running in the background and phoning home.",
            Run = r =>
            {
                r.Shell("taskkill /f /im msedge.exe");
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Edge", "BackgroundModeEnabled", 0);
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Edge", "StartupBoostEnabled", 0);
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Edge", "EdgeCollectionsEnabled", 0);
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Edge", "PersonalizationReportingEnabled", 0);
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Edge", "UserFeedbackAllowed", 0);
                r.DisableTask(@"\MicrosoftEdgeUpdateTaskMachineCore");
                r.DisableTask(@"\MicrosoftEdgeUpdateTaskMachineUA");
                r.DisableService("edgeupdate");
                r.DisableService("edgeupdatem");
            }
        });

        // --- Xbox: strip everything the average game does not require ---
        a.Add(ActionRegistry.App("Xbox Game Bar overlay", "XboxGamingOverlay", "Remove the Game Bar overlay (Win+G)."));
        a.Add(ActionRegistry.App("Xbox Game Bar Plugin", "XboxGameOverlay", "Remove Game Bar overlay plugin."));
        a.Add(ActionRegistry.App("Xbox Speech To Text", "XboxSpeechToTextOverlay", "Remove Game Bar speech overlay."));
        a.Add(ActionRegistry.App("Xbox Identity Provider", "Xbox.TCUI", "Remove Xbox TCUI (kept OFF nuke, some games use it).", nuke: false));
        a.Add(ActionRegistry.App("Xbox App", "GamingApp", "Remove the Xbox app itself (not required to launch most games)."));
        a.Add(ActionRegistry.App("Xbox Console Companion", "XboxApp", "Remove legacy Xbox Console Companion."));

        // --- Store cruft / bundled bloat (individual toggles) ---
        (string name, string pat, string desc, bool nuke)[] apps =
        {
            ("Cortana",              "549981C3F5F10", "Remove Cortana assistant.", true),
            ("Bing News",            "BingNews",       "Remove the News app.", true),
            ("Bing Weather",         "BingWeather",    "Remove the Weather app.", true),
            ("Bing Search",          "BingSearch",     "Remove the web-search shell component.", true),
            ("Get Help",             "GetHelp",        "Remove the Get Help app.", true),
            ("Get Started / Tips",   "Getstarted",     "Remove the Tips app.", true),
            ("Feedback Hub",         "WindowsFeedbackHub", "Remove Feedback Hub.", true),
            ("Maps",                 "WindowsMaps",    "Remove the Maps app.", true),
            ("Solitaire Collection", "SolitaireCollection", "Remove Microsoft Solitaire.", true),
            ("Sticky Notes",         "MicrosoftStickyNotes", "Remove Sticky Notes.", true),
            ("To Do",                "Todos",          "Remove Microsoft To Do.", true),
            ("Power Automate",       "PowerAutomateDesktop", "Remove Power Automate Desktop.", true),
            ("Teams (personal)",     "MicrosoftTeams", "Remove consumer Teams / Chat.", true),
            ("Teams (MSTeams)",      "MSTeams",        "Remove the new Teams package.", true),
            ("Clipchamp",            "Clipchamp",      "Remove the Clipchamp video editor.", true),
            ("Office Hub",           "MicrosoftOfficeHub", "Remove the Office promo hub.", true),
            ("Outlook (new)",        "OutlookForWindows", "Remove the new Outlook web app.", true),
            ("Family / Safety",      "Family",         "Remove Microsoft Family.", true),
            ("Dev Home",             "DevHome",        "Remove Dev Home.", true),
            ("Quick Assist",         "QuickAssist",    "Remove Quick Assist remote help.", true),
            ("People",               "People",         "Remove the People app.", true),
            ("Your Phone / Link",    "YourPhone",      "Remove Phone Link.", true),
            ("Mail & Calendar",      "windowscommunicationsapps", "Remove Mail and Calendar.", true),
            ("Skype",                "SkypeApp",       "Remove Skype.", true),
            ("Mixed Reality Portal", "MixedReality.Portal", "Remove Mixed Reality Portal.", true),
            ("3D Viewer",            "Microsoft3DViewer", "Remove 3D Viewer.", true),
            ("Paint 3D",             "MSPaint",        "Remove Paint 3D.", true),
            ("Alarms & Clock",       "WindowsAlarms",  "Remove Alarms & Clock.", true),
            ("Sound Recorder",       "WindowsSoundRecorder", "Remove Voice Recorder.", true),
            ("Movies & TV",          "ZuneVideo",      "Remove Movies & TV.", true),
            ("Media Player (Zune)",  "ZuneMusic",      "Remove the Media Player / Groove.", false),
            ("Camera",               "WindowsCamera",  "Remove the Camera app.", false),
            ("Photos",               "Windows.Photos", "Remove the Photos app (OFF by default).", false),
            ("Calculator",           "WindowsCalculator", "Remove Calculator (OFF by default).", false),
            ("Snipping Tool",        "ScreenSketch",   "Remove Snipping Tool (OFF by default).", false),
            ("Notepad",              "WindowsNotepad", "Remove Notepad (OFF by default).", false),
            ("Terminal",             "WindowsTerminal", "Remove Windows Terminal (OFF by default).", false),
        };
        foreach (var (name, pat, desc, nuke) in apps)
            a.Add(ActionRegistry.App(name, pat, desc, nuke));

        // --- Store itself + content delivery ---
        a.Add(new TweakAction
        {
            Id = "app_store_ads", Category = Category.Apps,
            Name = "Store suggestions & auto-install", Desc = "Stop the Store silently reinstalling promoted apps.",
            Run = r =>
            {
                string cd = @"HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
                foreach (var v in new[] { "SilentInstalledAppsEnabled", "PreInstalledAppsEnabled",
                    "OemPreInstalledAppsEnabled", "ContentDeliveryAllowed", "SubscribedContentEnabled",
                    "SystemPaneSuggestionsEnabled", "SoftLandingEnabled", "RotatingLockScreenEnabled" })
                    r.RegDword(cd, v, 0);
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1);
            }
        });

        return a;
    }
}
