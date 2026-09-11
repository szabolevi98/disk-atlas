using System.Diagnostics;
using System.Security.Principal;

namespace DiskAtlas;

/// <summary>
/// Opening a volume as a raw device needs administrator rights. The application starts
/// unelevated so it stays easy to debug, and offers to restart itself when a scan needs
/// the privilege.
/// </summary>
internal static class Elevation
{
    public static bool IsElevated { get; } = DetectElevation();

    private static bool DetectElevation()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Relaunches the current executable with the runas verb, which is what raises the
    /// UAC prompt.
    /// </summary>
    /// <returns>False when the user dismissed the prompt or the process could not start.</returns>
    public static bool TryRestartElevated()
    {
        string? executable = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executable))
        {
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = AppContext.BaseDirectory,
        };

        try
        {
            return Process.Start(startInfo) is not null;
        }
        catch (Exception)
        {
            // The most common case is the user clicking No on the UAC dialog.
            return false;
        }
    }
}
