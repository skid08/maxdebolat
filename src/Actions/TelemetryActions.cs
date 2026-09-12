namespace MaxDebloat;

internal static class TelemetryActions
{
    public static IEnumerable<TweakAction> Get()
    {
        var a = new List<TweakAction>();

        a.Add(new TweakAction
        {
            Id = "tel_level", Category = Category.Telemetry,
            Name = "Set telemetry to Security/0", Desc = "Force the lowest data-collection level via policy.",
            Run = r =>
            {
                string dc = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection";
                r.RegDword(dc, "AllowTelemetry", 0);
                r.RegDword(dc, "AllowDeviceNameInTelemetry", 0);
                r.RegDword(dc, "DoNotShowFeedbackNotifications", 1);
                r.RegDword(dc, "DisableOneSettingsDownloads", 1);
                r.RegDword(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 0);
                r.RegDword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 0);
            }
        });

        a.Add(new TweakAction
        {
            Id = "tel_advertising", Category = Category.Telemetry,
            Name = "Kill Advertising ID", Desc = "Disable the per-user advertising identifier.",
            Run = r =>
            {
                r.RegDword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0);
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", "DisabledByGroupPolicy", 1);
            }
        });

        a.Add(new TweakAction
        {
            Id = "tel_feedback", Category = Category.Telemetry,
            Name = "Disable feedback & tailored exp.", Desc = "No feedback prompts, no tailored experiences.",
            Run = r =>
            {
                r.RegDword(@"HKCU\Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", 0);
                r.RegDword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 0);
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableTailoredExperiencesWithDiagnosticData", 1);
            }
        });

        a.Add(new TweakAction
        {
            Id = "tel_activity", Category = Category.Telemetry,
            Name = "Disable Activity History", Desc = "Stop collecting/uploading the Timeline activity feed.",
            Run = r =>
            {
                string sys = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System";
                r.RegDword(sys, "EnableActivityFeed", 0);
                r.RegDword(sys, "PublishUserActivities", 0);
                r.RegDword(sys, "UploadUserActivities", 0);
            }
        });

        a.Add(new TweakAction
        {
            Id = "tel_location", Category = Category.Telemetry,
            Name = "Disable location tracking", Desc = "Deny system-wide location access.",
            Run = r =>
            {
                r.RegSz(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location", "Value", "Deny");
                r.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 1);
            }
        });

        a.Add(new TweakAction
        {
            Id = "tel_typing", Category = Category.Telemetry,
            Name = "Disable typing/inking telemetry", Desc = "No handwriting/typing data collection.",
            Run = r =>
            {
                r.RegDword(@"HKCU\Software\Microsoft\Input\TIPC", "Enabled", 0);
                r.RegDword(@"HKCU\Software\Microsoft\InputPersonalization", "RestrictImplicitInkCollection", 1);
                r.RegDword(@"HKCU\Software\Microsoft\InputPersonalization", "RestrictImplicitTextCollection", 1);
                r.RegDword(@"HKCU\Software\Microsoft\Personalization\Settings", "AcceptedPrivacyPolicy", 0);
            }
        });

        a.Add(new TweakAction
        {
            Id = "tel_cloud", Category = Category.Telemetry,
            Name = "Disable cloud content & spotlight", Desc = "No Spotlight, no consumer feature pushes.",
            Run = r =>
            {
                string cc = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent";
                r.RegDword(cc, "DisableWindowsConsumerFeatures", 1);
                r.RegDword(cc, "DisableSoftLanding", 1);
                r.RegDword(cc, "DisableCloudOptimizedContent", 1);
                r.RegDword(cc, "DisableThirdPartySuggestions", 1);
            }
        });

        a.Add(new TweakAction
        {
            Id = "tel_wifi_sense", Category = Category.Telemetry,
            Name = "Disable Wi-Fi Sense", Desc = "No automatic hotspot/credential sharing.",
            Run = r =>
            {
                string wifi = @"HKLM\SOFTWARE\Microsoft\PolicyManager\default\WiFi";
                r.RegDword(wifi + @"\AllowWiFiHotSpotReporting", "value", 0);
                r.RegDword(wifi + @"\AllowAutoConnectToWiFiSenseHotspots", "value", 0);
            }
        });

        a.Add(new TweakAction
        {
            Id = "tel_autologgers", Category = Category.Telemetry,
            Name = "Stop telemetry ETW autologgers", Desc = "Disable AutoLogger tracing sessions that feed telemetry.",
            Run = r =>
            {
                r.RegDword(@"HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\AutoLogger-Diagtrack-Listener", "Start", 0);
                r.RegDword(@"HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SQMLogger", "Start", 0);
                r.Shell(@"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\AutoLogger-Diagtrack-Listener\{5f8571df-c6f5-4bc0-a19b-14fecb31f7a5}"" /f");
            }
        });

        return a;
    }
}
