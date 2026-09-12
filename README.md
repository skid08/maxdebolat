# ☠ MAX DEBLOAT

A standalone Windows 11 debloating & tweaking utility. Strips Windows 11 down as far as it
physically goes — minimum processes, minimum services (**hard target: under 30 running services
after a full run**), tuned as a pure gaming machine.

> **This is extreme and one-way. It is designed for a throwaway VM.**
> There is **no backup, no restore, no undo**. It removes Windows Defender, telemetry, and dozens
> of services and apps in a single pass. Do not run it on a machine you care about.

---

## 1. Language / framework choice

**C# / .NET 8 WinForms, published as a self-contained single-file `win-x64` executable.**

*One-line justification:* it compiles to **one portable `.exe` with the runtime embedded** (no
installer, no separately-downloaded runtime), and WinForms renders natively through GDI so it
launches reliably on a **clean Windows 11 VM** with no OpenGL/WebView2/browser dependency, while
still giving full control over a custom dark UI.

The entire runtime is bundled into the executable, so on the target machine you literally
double-click `MaxDebloat.exe` and it runs. It requests administrator rights automatically (via the
embedded manifest); nothing here works without them.

---

## 2. Building

Requirements: **.NET 8 SDK** (the SDK, not just the runtime), on Windows.
Get it at <https://dotnet.microsoft.com/download/dotnet/8.0>.

### Easiest

```powershell
.\build.ps1
```

### Manual

```powershell
dotnet publish .\MaxDebloat.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
  -o .\publish
```

The result is a single file at:

```
.\publish\MaxDebloat.exe
```

It is ~70–150 MB because the .NET runtime is embedded — that is the price of the "no external
dependencies" requirement. Copy that one file to the VM and run it.

> Cross-compiling from Linux/macOS works too (`dotnet publish` restores the win-x64 runtime pack),
> but you still get a Windows `.exe`; it can only be *run* on Windows.

---

## 3. How it works

- **Dark, dense, utilitarian desktop UI** — custom-drawn borderless window, sharp 1px borders,
  owner-drawn toggle switches and buttons. Not a web view.
- **Left nav** groups every action into eight categories: **Apps, Services, Telemetry,
  Defender/Security, Gaming Tweaks, Scheduled Tasks, Startup, Network.**
- **Every action is its own toggle** with an individual **RUN** button. Toggles marked `NUKE` are
  part of the one-click profile; toggles marked `OPT` are opt-in (things that can break a real
  gaming setup, e.g. Xbox Live services, Remote Desktop, IPv6, Windows Update).
- **APPLY SELECTED** runs exactly what you have toggled (pre-seeded to the recommended profile).
- **ONE-CLICK NUKE** (big red button, top-left) applies the full extreme profile in a single pass.
- **One confirmation prompt**, then it executes. No repeated "are you sure" nagging. Individual
  RUN buttons execute immediately with no prompt (power-user mode).
- A live **snapshot** (running services / process count) sits in the top bar; the service count
  turns green when it drops below 30. Hit **REFRESH SNAPSHOT** after a run to watch it fall.
- The **output log** at the bottom streams every command as it runs.

### Source layout

```
MaxDebloat.csproj        single-file self-contained publish config
app.manifest             requireAdministrator + Win11 compat + PerMonitor DPI
build.ps1                one-command build
src/
  Program.cs             entry point
  Theme.cs               dark palette + fonts
  MainForm.cs            the whole UI (nav, rows, log, snapshot, window chrome)
  Core/
    Runner.cs            process/registry/service/appx/task execution + logging
    TweakAction.cs       action + category model
  Controls/
    ToggleSwitch.cs      owner-drawn on/off switch
    FlatButton.cs        owner-drawn flat button
    ConfirmDialog.cs     the single dark confirmation modal
  Actions/
    Registry.cs          builds the full catalog + helpers
    AppsActions.cs       Store apps, Copilot, Widgets, Xbox, OneDrive, Edge
    ServicesActions.cs   the service kill-list (drives the <30 target)
    TelemetryActions.cs  telemetry / tracking / advertising / activity history
    DefenderActions.cs   Defender neutralization (takes ownership of protected keys)
    GamingActions.cs     power plan, GPU scheduling, latency/priority tweaks
    TasksActions.cs      scheduled-task kill-list
    StartupActions.cs    startup entries + background apps
    NetworkActions.cs    hosts block, LLMNR, SMBv1, NetBIOS, throttling, Nagle
```

---

## 4. What the "One-Click Nuke" profile touches

Every action tagged `NUKE` in the UI, in one pass:

**Apps** — Removes Copilot (+ policy lockout), Widgets/Web-Experience (+ taskbar off), OneDrive
(uninstalled, purged from Explorer & startup), Edge background/telemetry processes + Edge update
tasks/services, the Xbox app + Game Bar overlay stack, and the Store-app bloat: Cortana, Bing
News/Weather/Search, Get Help, Tips, Feedback Hub, Maps, Solitaire, Sticky Notes, To Do, Power
Automate, Teams (both), Clipchamp, Office Hub, new Outlook, Family, Dev Home, Quick Assist, People,
Phone Link, Mail & Calendar, Skype, Mixed Reality Portal, 3D Viewer, Paint 3D, Alarms, Sound
Recorder, Movies & TV — plus disabling Store silent auto-install / suggestions.

**Services** — Disables (stop + `start=disabled` + registry `Start=4`): DiagTrack, dmwappushservice,
diagnostics-hub collector, WerSvc, Diagnostic Policy stack (DPS/WdiServiceHost/WdiSystemHost),
PcaSvc, **WSearch (indexing)**, **SysMain (Superfetch)**, Print Spooler, Fax, WIA/scanner,
Geolocation, Biometrics, Payments/NFC, Phone, Parental Controls, the Bluetooth stack, WMP network
sharing, Remote Registry, RRAS, ICS, ALG, AllJoyn, IP Helper, LLTD, SSDP/UPnP, Distributed Link
Tracking, Connected Devices Platform, the sync-host template services (mail/contacts/messaging),
Windows Insider, Retail Demo, Event Collector + WEP host. This is what drives the count under 30.
*(Left running: audio, networking core, RPC/DCOM, power, plug-and-play, crypto, WMI, task scheduler,
graphics, firewall/BFE, profile & group-policy, device install.)*

**Telemetry** — Telemetry level forced to 0, Advertising ID killed, feedback & tailored experiences
off, Activity History / Timeline off, location tracking denied, typing/inking telemetry off, cloud
content & Spotlight off, Wi-Fi Sense off, and the telemetry ETW autologgers stopped.

**Defender / Security** — Attempts Tamper Protection off; disables real-time protection, behavior
monitoring, IOAV, script/archive scanning; disables cloud (MAPS) reporting and sample submission;
**takes ownership of Defender's protected service keys** and forces WinDefend / WdNisSvc / Sense /
WdFilter / WdBoot / web-threat-defense to disabled; disables all Defender scheduled tasks; disables
SmartScreen everywhere; kills the Security Health tray + wscsvc/SecurityHealthService; and removes
the "Scan with Defender" context-menu entry.

**Gaming Tweaks** — Ultimate Performance power plan (+ sleep/hibernate off), CPU power throttling
off, Game DVR/recording off, Game Mode on, hardware-accelerated GPU scheduling on, MMCSS Games
profile tuned (max GPU/CPU priority, no network throttling), foreground priority boost
(Win32PrioritySeparation), mouse acceleration off, fullscreen optimizations off, visual effects set
to performance, memory compression off.

**Scheduled Tasks** — Disables Application Experience / Compatibility Appraiser, CEIP consolidator
+ USB/kernel CEIP + autochk proxy, Feedback (DmClient), Windows Error Reporting queue, disk
diagnostics collector, Maps update/toast, Clip license validation, CloudExperienceHost, power
efficiency diagnostics + device info, Family Safety monitor, and the speech-model download task.

**Startup** — Purges common Run-key entries (OneDrive, Teams, Edge, Spotify, Discord, …), disables
Edge startup boost, zeroes the Explorer startup delay, disables UWP background apps, and turns off
lock-screen tips/ads.

**Network** — Writes ~23 Microsoft telemetry/phone-home domains to the `hosts` file as `0.0.0.0`,
disables LLMNR, removes SMBv1, disables NetBIOS-over-TCP/IP on every interface, disables network
throttling, and disables Nagle's algorithm (TcpAckFrequency/TCPNoDelay) for lower latency.

### Deliberately **NOT** in the one-click profile (opt-in toggles)

Because they break things a real gaming machine may still want: **Xbox Live services** (saves /
multiplayer for some titles), **Remote Desktop** (you may RDP into the VM), **Windows Update
stack**, **IPv6**, **smart-card** and **touch-keyboard** services, and destructive app removals like
Calculator / Photos / Snipping Tool / Notepad / Terminal. Flip them on manually if you want them.

---

## 5. Note on Defender removal

On a current Windows 11 build with **Tamper Protection still ON**, Defender reverts some live
registry writes itself. For a *guaranteed* full kill, turn Tamper Protection off first
(Settings → Privacy & security → Windows Security → Virus & threat protection → Manage settings),
or run from an offline/WinPE context, then run the Defender actions. The tool takes ownership of
Defender's protected keys and does the maximum achievable from a live elevated session, and logs
clearly when a write is likely to be reverted.
