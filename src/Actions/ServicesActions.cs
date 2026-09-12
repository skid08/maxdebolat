namespace MaxDebloat;

/// <summary>
/// Disables non-essential services. Deliberately leaves the boot/audio/GPU/network core alone
/// (Audiosrv, Dhcp, Dnscache, NlaSvc, nsi, RpcSs, DcomLaunch, Power, PlugPlay, CryptSvc, Winmgmt,
/// Schedule, Themes, EventLog, mpssvc, BFE, gpsvc, ProfSvc, DeviceInstall, WlanSvc). Applying the
/// full set drops a stock Windows 11 install to well under 30 running services.
/// </summary>
internal static class ServicesActions
{
    public static IEnumerable<TweakAction> Get()
    {
        var a = new List<TweakAction>();

        // ---- BIGGEST process-count lever ----
        // On machines with >3.5 GB RAM, Windows runs every service in its OWN svchost.exe.
        // Raising the split threshold above installed RAM forces services to share a handful
        // of svchost processes instead. This alone typically removes 20-40 processes after a reboot.
        a.Add(new TweakAction
        {
            Id = "svc_group_svchost", Category = Category.Services,
            Name = "Group svchost processes (huge process cut)",
            Desc = "Force all shareable services into shared svchost.exe processes. REQUIRES REBOOT.",
            Run = r =>
            {
                r.RegDword(@"HKLM\SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB", 0xFFFFFFFF);
                r.Log("  svchost grouping set. This takes effect after the next reboot.", LogLevel.Warn);
            }
        });

        // Telemetry / diagnostics
        a.Add(ActionRegistry.Svc("Connected User Experiences (DiagTrack)", "DiagTrack", "The main Windows telemetry service."));
        a.Add(ActionRegistry.Svc("WAP Push Message Routing", "dmwappushservice", "Telemetry transport service."));
        a.Add(ActionRegistry.Svc("Diagnostics Hub Collector", "diagnosticshub.standardcollector.service", "ETW diagnostics data collector."));
        a.Add(ActionRegistry.Svc("Windows Error Reporting", "WerSvc", "Sends crash dumps to Microsoft."));
        a.Add(ActionRegistry.SvcGroup("diag", "Diagnostic Policy stack", new[] { "DPS", "WdiServiceHost", "WdiSystemHost" }, "Diagnostic policy + host services."));
        a.Add(ActionRegistry.Svc("Program Compatibility Assistant", "PcaSvc", "Tracks app compatibility (telemetry-adjacent)."));

        // Search / indexing / prefetch
        a.Add(ActionRegistry.Svc("Windows Search (indexing)", "WSearch", "Disk indexing service. Big CPU/disk win for gaming."));
        a.Add(ActionRegistry.Svc("SysMain / Superfetch", "SysMain", "Prefetch service; disable on SSD gaming rigs."));

        // Printing / scanning / fax
        a.Add(ActionRegistry.Svc("Print Spooler", "Spooler", "Printing. Not needed on a gaming box."));
        a.Add(ActionRegistry.Svc("Fax", "Fax", "Fax service."));
        a.Add(ActionRegistry.Svc("Windows Image Acquisition", "stisvc", "Scanner / still-image capture."));

        // Location / sensors / biometrics / payments
        a.Add(ActionRegistry.Svc("Geolocation", "lfsvc", "Location tracking service."));
        a.Add(ActionRegistry.Svc("Biometric Service", "WbioSrvc", "Windows Hello biometrics."));
        a.Add(ActionRegistry.Svc("Payments & NFC (SEMgr)", "SEMgrSvc", "Wallet / NFC payment manager."));
        a.Add(ActionRegistry.Svc("Phone Service", "PhoneSvc", "Telephony state for phone features."));
        a.Add(ActionRegistry.Svc("Parental Controls", "WpcMonSvc", "Family safety monitor."));

        // Bluetooth (irrelevant in a VM)
        a.Add(ActionRegistry.SvcGroup("bt", "Bluetooth stack", new[] { "bthserv", "BthAvctpSvc", "BluetoothUserService" }, "All Bluetooth services."));

        // Media sharing
        a.Add(ActionRegistry.Svc("WMP Network Sharing", "WMPNetworkSvc", "Streams WMP libraries on the network."));

        // Remote access surfaces
        a.Add(ActionRegistry.Svc("Remote Registry", "RemoteRegistry", "Remote registry editing. Attack surface."));
        a.Add(ActionRegistry.Svc("Routing and Remote Access", "RemoteAccess", "RRAS / VPN routing."));
        a.Add(ActionRegistry.Svc("Internet Connection Sharing", "SharedAccess", "ICS / NAT sharing."));
        a.Add(ActionRegistry.Svc("Application Layer Gateway", "ALG", "ALG for ICS."));
        a.Add(ActionRegistry.Svc("AllJoyn Router", "AJRouter", "IoT AllJoyn router."));
        // Remote Desktop OFF by default: you may RDP into the VM
        a.Add(ActionRegistry.SvcGroup("rdp", "Remote Desktop", new[] { "TermService", "SessionEnv", "UmRdpService" }, "RDP services. OFF by default (you may RDP in).", nuke: false));

        // Networking odds & ends
        a.Add(ActionRegistry.Svc("IP Helper (6to4/ISATAP)", "iphlpsvc", "IPv6 transition tunneling."));
        a.Add(ActionRegistry.Svc("Link-Layer Topology Discovery", "lltdsvc", "Network map discovery."));
        a.Add(ActionRegistry.SvcGroup("ssdp", "SSDP / UPnP", new[] { "SSDPSRV", "upnphost" }, "UPnP device discovery/host."));
        a.Add(ActionRegistry.Svc("Distributed Link Tracking", "TrkWks", "Tracks moved NTFS links."));

        // Sync / connected-devices / per-user template services
        a.Add(ActionRegistry.Svc("Connected Devices Platform", "CDPSvc", "Cross-device sync (clipboard/nearby)."));
        a.Add(ActionRegistry.SvcGroup("sync", "Sync host services", new[] { "OneSyncSvc", "MessagingService", "PimIndexMaintenanceSvc", "UserDataSvc", "UnistoreSvc" }, "Mail/contacts/messaging sync templates."));

        // Insider / demo / event collection
        a.Add(ActionRegistry.Svc("Windows Insider Service", "wisvc", "Insider preview flighting."));
        a.Add(ActionRegistry.Svc("Retail Demo", "RetailDemo", "Store demo-mode service."));
        a.Add(ActionRegistry.SvcGroup("wec", "Event Collector / WEP host", new[] { "Wecsvc", "WEPHOSTSVC" }, "Event forwarding + encrypted-provider host."));

        // Smart card (unused in VM)
        a.Add(ActionRegistry.SvcGroup("smartcard", "Smart Card stack", new[] { "SCardSvr", "ScDeviceEnum", "SCPolicySvc" }, "Smart card services.", nuke: false));

        // Touch keyboard / handwriting (OFF by default: can affect some input)
        a.Add(ActionRegistry.Svc("Touch Keyboard & Handwriting", "TabletInputService", "Touch keyboard panel. OFF by default.", nuke: false));

        // Xbox Live (OFF by default: some games need saves/multiplayer via Xbox Live)
        a.Add(ActionRegistry.SvcGroup("xbox", "Xbox Live services", new[] { "XblAuthManager", "XblGameSave", "XboxGipSvc", "XboxNetApiSvc" }, "Xbox Live auth/save/net. OFF by default — some games use these.", nuke: false));

        // Update stack (OFF by default: you likely still want security patches even on a gaming VM)
        a.Add(ActionRegistry.SvcGroup("update", "Windows Update stack", new[] { "wuauserv", "UsoSvc", "WaaSMedicSvc", "BITS", "DoSvc" }, "Update + delivery-optimization services. OFF by default (breaks updates).", nuke: false));

        // ---- Deeper cuts to push process count down further ----
        a.Add(ActionRegistry.Svc("Problem Reports control panel", "wercplsupport", "Problem Reports support service."));
        a.Add(ActionRegistry.Svc("Data Usage", "DusmSvc", "Per-app network data usage tracking."));
        a.Add(ActionRegistry.Svc("Shell Hardware Detection", "ShellHWDetection", "AutoPlay / removable-media detection."));
        a.Add(ActionRegistry.Svc("Network Connection Broker", "NcbService", "Background network for UWP apps."));
        a.Add(ActionRegistry.Svc("Push Notifications", "WpnService", "Windows toast/push notification system."));
        a.Add(ActionRegistry.Svc("Device Setup Manager", "DsmSvc", "Downloads driver metadata for new devices."));
        a.Add(ActionRegistry.Svc("Device Management Enrollment", "DmEnrollmentSvc", "MDM / workplace enrollment."));
        a.Add(ActionRegistry.Svc("Payments/SmartCard PnP", "ScDeviceEnum", "Smart card device enumeration."));
        a.Add(ActionRegistry.Svc("Sensor Service", "SensorService", "Ambient light / orientation sensors."));
        a.Add(ActionRegistry.Svc("Offline Files", "CscService", "Offline file caching.", nuke: false));

        // Store / licensing / account (safe once apps are removed, but OFF by default:
        // disabling these blocks Store, Store-app launch and Microsoft-account sign-in)
        a.Add(ActionRegistry.SvcGroup("store", "Store & licensing", new[] { "InstallService", "LicenseManager", "ClipSVC", "AppXSvc" }, "Store install/licensing. OFF by default (breaks the Store & UWP apps).", nuke: false));
        a.Add(ActionRegistry.SvcGroup("account", "MS account / token broker", new[] { "wlidsvc", "TokenBroker", "DeviceAssociationService" }, "Microsoft-account + token broker. OFF by default (breaks MS-account sign-in).", nuke: false));
        a.Add(ActionRegistry.Svc("Windows Time", "W32Time", "Clock sync. OFF by default (clock may drift).", nuke: false));

        return a;
    }
}
