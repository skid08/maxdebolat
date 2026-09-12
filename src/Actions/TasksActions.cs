namespace MaxDebloat;

internal static class TasksActions
{
    public static IEnumerable<TweakAction> Get()
    {
        var a = new List<TweakAction>();

        a.Add(ActionRegistry.TaskGroup("appexp", "Application Experience (CEIP)",
            new[]
            {
                @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                @"\Microsoft\Windows\Application Experience\ProgramDataUpdater",
                @"\Microsoft\Windows\Application Experience\StartupAppTask",
                @"\Microsoft\Windows\Application Experience\PcaPatchDbTask",
                @"\Microsoft\Windows\Application Experience\MareBackup",
            },
            "Compatibility appraiser + program-data telemetry tasks."));

        a.Add(ActionRegistry.TaskGroup("ceip", "Customer Experience Program",
            new[]
            {
                @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
                @"\Microsoft\Windows\Customer Experience Improvement Program\KernelCeipTask",
                @"\Microsoft\Windows\Autochk\Proxy",
            },
            "CEIP consolidator, USB CEIP, kernel CEIP, autochk proxy."));

        a.Add(ActionRegistry.Task("Feedback (DmClient)",
            @"\Microsoft\Windows\Feedback\Siuf\DmClient",
            "Feedback upload task."));
        a.Add(ActionRegistry.Task("Feedback (DmClientOnScenarioDownload)",
            @"\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload",
            "Feedback scenario download task."));

        a.Add(ActionRegistry.Task("Error Reporting queue",
            @"\Microsoft\Windows\Windows Error Reporting\QueueReporting",
            "Uploads queued crash reports."));

        a.Add(ActionRegistry.TaskGroup("diskdiag", "Disk diagnostics data collector",
            new[] { @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector" },
            "SMART/disk diagnostic telemetry collector."));

        a.Add(ActionRegistry.Task("Program Data Updater (Maps)",
            @"\Microsoft\Windows\Maps\MapsUpdateTask",
            "Offline maps update task."));
        a.Add(ActionRegistry.Task("Maps toast",
            @"\Microsoft\Windows\Maps\MapsToastTask",
            "Maps notification task."));

        a.Add(ActionRegistry.Task("Clip license validation",
            @"\Microsoft\Windows\Clip\License Validation",
            "Store license validation."));

        a.Add(ActionRegistry.Task("CloudExperienceHost createobject",
            @"\Microsoft\Windows\CloudExperienceHost\CreateObjectTask",
            "OOBE/cloud experience helper."));

        a.Add(ActionRegistry.TaskGroup("power", "Power efficiency diagnostics",
            new[]
            {
                @"\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem",
                @"\Microsoft\Windows\Device Information\Device",
                @"\Microsoft\Windows\Device Information\Device User",
            },
            "Power analysis + device-info collection tasks."));

        a.Add(ActionRegistry.Task("Family Safety monitor",
            @"\Microsoft\Windows\Shell\FamilySafetyMonitor",
            "Parental controls task."));

        a.Add(ActionRegistry.TaskGroup("wifi", "WiFi task offload",
            new[]
            {
                @"\Microsoft\Windows\WCM\WiFiTask",
                @"\Microsoft\Windows\WlanSvc\CDSSync",
            },
            "WiFi background sync tasks.", nuke: false));

        a.Add(ActionRegistry.Task("Speech model download",
            @"\Microsoft\Windows\Speech\SpeechModelDownloadTask",
            "Downloads speech recognition models."));

        return a;
    }
}
