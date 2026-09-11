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
