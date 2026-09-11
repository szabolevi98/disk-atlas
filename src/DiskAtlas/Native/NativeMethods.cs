using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace DiskAtlas.Native;

/// <summary>
/// The Win32 entry points needed to read a volume as a raw device.
/// </summary>
internal static partial class NativeMethods
{
    internal const uint GENERIC_READ = 0x80000000;
    internal const uint FILE_SHARE_READ = 0x00000001;
    internal const uint FILE_SHARE_WRITE = 0x00000002;
    internal const uint OPEN_EXISTING = 3;
    internal const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
    internal const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;

    /// <summary>
    /// Opts the process into dark common controls. Scroll bars, headers and other pieces
    /// the system draws itself stay light otherwise, whatever colours the managed control
    /// is given. The entry points are exported by ordinal only, so they are bound that way.
    /// </summary>
    internal static void UseDarkCommonControls()
    {
        try
        {
            SetPreferredAppMode(AllowDarkMode);
            FlushMenuThemes();
        }
        catch (EntryPointNotFoundException)
        {
            // Windows 10 builds before 1903 do not export this.
        }
        catch (DllNotFoundException)
        {
        }
    }

    /// <summary>
    /// Switches one control to the dark visual style, which is what turns its scroll bars
    /// and header from white to dark.
    /// </summary>
    internal static void UseDarkStyle(nint control)
    {
        try
        {
            SetWindowTheme(control, "DarkMode_Explorer", nint.Zero);
        }
        catch (DllNotFoundException)
        {
        }
    }

    /// <summary>Allow dark mode where the application asks for it.</summary>
    private const int AllowDarkMode = 1;

    [LibraryImport("uxtheme.dll", EntryPoint = "#135", SetLastError = false)]
    private static partial int SetPreferredAppMode(int mode);

    [LibraryImport("uxtheme.dll", EntryPoint = "#136", SetLastError = false)]
    private static partial void FlushMenuThemes();

    [LibraryImport("uxtheme.dll", EntryPoint = "SetWindowTheme", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SetWindowTheme(nint window, string subAppName, nint subIdList);

    /// <summary>Windows 11 attribute that paints the title bar to match a dark window.</summary>
    private const int UseImmersiveDarkMode = 20;

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);

    /// <summary>
    /// Asks the desktop compositor for a dark title bar. Older builds simply ignore the
    /// attribute, so the failure is not worth reporting.
    /// </summary>
    internal static void UseDarkTitleBar(nint window)
    {
        int enabled = 1;

        try
        {
            DwmSetWindowAttribute(window, UseImmersiveDarkMode, ref enabled, sizeof(int));

            // The frame is only repainted when it is told the style changed, so without
            // this the title bar keeps its light colours until the window is resized.
            SetWindowPos(
                window, nint.Zero, 0, 0, 0, 0,
                SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(
        nint window, nint insertAfter, int x, int y, int width, int height, uint flags);

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        nint securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        nint templateFile);

    /// <summary>
    /// Opens a volume such as <c>C:</c> for raw reading. The trailing backslash must be
    /// omitted, otherwise Windows opens the root directory instead of the device.
    /// </summary>
    internal static SafeFileHandle OpenVolume(string driveLetter)
    {
        string path = $@"\\.\{driveLetter.TrimEnd('\\', ':')}:";

        SafeFileHandle handle = CreateFile(
            path,
            GENERIC_READ,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            nint.Zero,
            OPEN_EXISTING,
            FILE_FLAG_SEQUENTIAL_SCAN,
            nint.Zero);

        if (handle.IsInvalid)
        {
            throw new System.ComponentModel.Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Could not open volume {path} for raw reading.");
        }

        return handle;
    }
}
