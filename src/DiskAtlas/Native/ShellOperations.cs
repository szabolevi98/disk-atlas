using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DiskAtlas.Native;

/// <summary>
/// The two shell operations the context menus need: revealing something in Explorer, and
/// deleting it. Deleting goes through the shell rather than the file API so the item
/// lands in the recycle bin and stays recoverable.
/// </summary>
internal static class ShellOperations
{
    private const uint FoDelete = 0x0003;

    /// <summary>Put the item in the recycle bin instead of destroying it.</summary>
    private const ushort FofAllowUndo = 0x0040;

    /// <summary>The application asks first, so the shell does not ask a second time.</summary>
    private const ushort FofNoConfirmation = 0x0010;

    /// <summary>
    /// Still warn when the item cannot be recycled and would be destroyed. This partially
    /// overrides the flag above, which is exactly what is wanted: the only prompt left is
    /// the one that matters.
    /// </summary>
    private const ushort FofWantNukeWarning = 0x4000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
    private struct ShFileOpStruct
    {
        public nint hwnd;
        public uint wFunc;
        public string pFrom;
        public string? pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fAnyOperationsAborted;
        public nint hNameMappings;
        public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHFileOperationW")]
    private static extern int SHFileOperation(ref ShFileOpStruct operation);

    /// <summary>What came of a delete the user asked for.</summary>
    internal enum DeleteOutcome
    {
        Deleted,
        Cancelled,
        Failed,
    }

    /// <summary>
    /// Sends one file or folder to the recycle bin. Folders go with everything inside
    /// them, which is what the shell does for a folder anywhere else in Windows.
    /// </summary>
    internal static DeleteOutcome Recycle(nint owner, string path, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "The item has no path on disk.";
            return DeleteOutcome.Failed;
        }

        var operation = new ShFileOpStruct
        {
            hwnd = owner,
            wFunc = FoDelete,

            // The source is a list of paths, so it ends with a second null terminator.
            pFrom = path + '\0' + '\0',
            fFlags = FofAllowUndo | FofNoConfirmation | FofWantNukeWarning,
        };

        int result = SHFileOperation(ref operation);

        if (operation.fAnyOperationsAborted)
        {
            return DeleteOutcome.Cancelled;
        }

        if (result != 0)
        {
            // The shell returns its own codes here, not Win32 ones, so the number is more
            // use to the reader than a translated message would be.
            error = $"The shell refused to delete the item (code 0x{result:X}).";
            return DeleteOutcome.Failed;
        }

        return DeleteOutcome.Deleted;
    }

    /// <summary>
    /// Opens a folder in Explorer, or opens the folder holding a file with that file
    /// selected.
    /// </summary>
    internal static bool Reveal(string path, bool isDirectory, out string? error)
    {
        error = null;

        try
        {
            ProcessStartInfo start = isDirectory
                ? new ProcessStartInfo("explorer.exe", $"\"{path}\"")
                : new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"");

            start.UseShellExecute = true;
            Process.Start(start);
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }
}
