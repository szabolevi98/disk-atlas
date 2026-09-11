namespace DiskAtlas.Model;

/// <summary>A fixed drive the application can scan.</summary>
public sealed class VolumeInfo
{
    private VolumeInfo(string driveLetter, string label, string fileSystem, long totalSize, long freeSpace)
    {
        DriveLetter = driveLetter;
        Label = label;
        FileSystem = fileSystem;
        TotalSize = totalSize;
        FreeSpace = freeSpace;
    }

    /// <summary>Single letter without a colon, for example <c>C</c>.</summary>
    public string DriveLetter { get; }

    public string Label { get; }

    public string FileSystem { get; }

    public long TotalSize { get; }

    public long FreeSpace { get; }

    public long UsedSpace => TotalSize - FreeSpace;

    /// <summary>
    /// Only NTFS can be read through the Master File Table. Other file systems need the
    /// slower directory walk, which is not implemented yet.
    /// </summary>
    public bool SupportsFastScan => FileSystem.Equals("NTFS", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<VolumeInfo> EnumerateReadyVolumes()
    {
        List<VolumeInfo> volumes = [];

        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady || drive.DriveType is not (DriveType.Fixed or DriveType.Removable))
            {
                continue;
            }

            try
            {
                volumes.Add(new VolumeInfo(
                    drive.Name[..1],
                    string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Local disk" : drive.VolumeLabel,
                    drive.DriveFormat,
                    drive.TotalSize,
                    drive.AvailableFreeSpace));
            }
            catch (IOException)
            {
                // A drive can disappear between the enumeration and the query.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return volumes;
    }

    public override string ToString() =>
        $"{DriveLetter}: — {Label} ({FileSystem}, {DiskNode.FormatSize(UsedSpace)} used of {DiskNode.FormatSize(TotalSize)})";
}
