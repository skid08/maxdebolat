namespace MaxDebloat;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // DPI mode, visual styles and text rendering are configured from the .csproj
        // (ApplicationHighDpiMode / manifest) via the generated initializer.
        ApplicationConfiguration.Initialize();

        Application.ThreadException += (_, e) => Report(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Report(e.ExceptionObject as Exception);

        Application.Run(new MainForm());
    }

    private static void Report(Exception? ex)
    {
        if (ex == null) return;
        MessageBox.Show(ex.ToString(), "MAX DEBLOAT — error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
