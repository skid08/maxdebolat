namespace MaxDebloat;

/// <summary>Builds the full catalog of debloat actions across every category.</summary>
internal static class ActionRegistry
{
    private static List<TweakAction>? _all;

    public static List<TweakAction> All => _all ??= Build();

    public static IEnumerable<TweakAction> InCategory(Category c) => All.Where(a => a.Category == c);

    public static IEnumerable<TweakAction> NukeProfile => All.Where(a => a.InNuke);

    private static List<TweakAction> Build()
    {
        var list = new List<TweakAction>();
        list.AddRange(AppsActions.Get());
        list.AddRange(ServicesActions.Get());
        list.AddRange(TelemetryActions.Get());
        list.AddRange(DefenderActions.Get());
        list.AddRange(GamingActions.Get());
        list.AddRange(TasksActions.Get());
        list.AddRange(StartupActions.Get());
        list.AddRange(NetworkActions.Get());
        return list;
    }

    // Small helpers used by the category files to keep them terse.
    public static TweakAction App(string name, string pattern, string desc, bool nuke = true) => new()
    {
        Id = "app_" + pattern, Category = Category.Apps, Name = name, Desc = desc, InNuke = nuke,
        Run = r => r.RemoveAppx(pattern)
    };

    public static TweakAction Svc(string display, string service, string desc, bool nuke = true) => new()
    {
        Id = "svc_" + service, Category = Category.Services, Name = display, Desc = desc, InNuke = nuke,
        Run = r => r.DisableService(service)
    };

    public static TweakAction SvcGroup(string id, string display, string[] services, string desc, bool nuke = true) => new()
    {
        Id = "svcgrp_" + id, Category = Category.Services, Name = display, Desc = desc, InNuke = nuke,
        Run = r => { foreach (var s in services) r.DisableService(s); }
    };

    public static TweakAction Task(string display, string path, string desc, bool nuke = true) => new()
    {
        Id = "task_" + display.Replace(' ', '_'), Category = Category.Tasks, Name = display, Desc = desc, InNuke = nuke,
        Run = r => r.DisableTask(path)
    };

    public static TweakAction TaskGroup(string id, string display, string[] paths, string desc, bool nuke = true) => new()
    {
        Id = "taskgrp_" + id, Category = Category.Tasks, Name = display, Desc = desc, InNuke = nuke,
        Run = r => { foreach (var p in paths) r.DisableTask(p); }
    };
}
