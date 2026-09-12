namespace MaxDebloat;

internal enum Category
{
    Apps,
    Services,
    Telemetry,
    Defender,
    Gaming,
    Tasks,
    Startup,
    Network
}

internal static class CategoryInfo
{
    public static readonly Category[] Order =
    {
        Category.Apps, Category.Services, Category.Telemetry, Category.Defender,
        Category.Gaming, Category.Tasks, Category.Startup, Category.Network
    };

    public static string Label(Category c) => c switch
    {
        Category.Apps      => "APPS",
        Category.Services  => "SERVICES",
        Category.Telemetry => "TELEMETRY",
        Category.Defender  => "DEFENDER / SEC",
        Category.Gaming    => "GAMING TWEAKS",
        Category.Tasks     => "SCHED. TASKS",
        Category.Startup   => "STARTUP",
        Category.Network   => "NETWORK",
        _ => c.ToString().ToUpperInvariant()
    };

    public static string Blurb(Category c) => c switch
    {
        Category.Apps      => "Rip out Store apps, Copilot, Widgets, Xbox extras, OneDrive, Edge.",
        Category.Services  => "Disable non-essential services. Target: under 30 running.",
        Category.Telemetry => "Kill tracking, data collection, advertising ID, activity history.",
        Category.Defender  => "Neutralize Windows Defender: service, real-time, tasks. One-way.",
        Category.Gaming    => "Max performance power plan, GPU scheduling, latency tweaks.",
        Category.Tasks     => "Disable telemetry / maintenance scheduled tasks.",
        Category.Startup   => "Strip startup entries that slow boot and phone home.",
        Category.Network   => "Kill telemetry hosts, LLMNR, SMBv1, network throttling.",
        _ => ""
    };
}

/// <summary>One discrete debloat operation, surfaced as a single toggle + RUN button.</summary>
internal sealed class TweakAction
{
    public required string Id;
    public required Category Category;
    public required string Name;
    public required string Desc;
    public bool InNuke = true;               // included in the One-Click Nuke profile
    public required Action<Runner> Run;
}
