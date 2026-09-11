namespace DiskAtlas;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Has to run before any window exists, otherwise the common controls keep
        // the light theme they were created with.
        Native.NativeMethods.UseDarkCommonControls();

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
