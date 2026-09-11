using System.Diagnostics;
using DiskAtlas.Model;
using DiskAtlas.Ntfs;

namespace DiskAtlas.Scanning;

/// <summary>Everything a finished scan produced.</summary>
public sealed class ScanResult
{
    internal ScanResult(DiskNode root, VolumeInfo volume, long fileCount, long directoryCount, TimeSpan duration)
    {
        Root = root;
        Volume = volume;
        FileCount = fileCount;
        DirectoryCount = directoryCount;
        Duration = duration;
    }

    public DiskNode Root { get; }

    public VolumeInfo Volume { get; }

    public long FileCount { get; }

    public long DirectoryCount { get; }

    public TimeSpan Duration { get; }
}

/// <summary>Reads a volume and hands back a ready to display tree.</summary>
public static class DiskScanner
{
    public static Task<ScanResult> ScanAsync(
        VolumeInfo volume,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(volume);

        if (!volume.SupportsFastScan)
        {
            throw new NotSupportedException(
                $"{volume.DriveLetter}: is {volume.FileSystem}. Only NTFS volumes can be read through the Master File Table.");
        }

        return Task.Run(
            () =>
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                IProgress<ScanProgress>? inner = progress is null
                    ? null
                    : new Progress<ScanProgress>(value => progress.Report(value.Fraction));

                RawScan raw = new MftScanner(volume.DriveLetter).Scan(inner, cancellationToken);
                DiskNode root = TreeBuilder.Build(raw, $"{volume.DriveLetter}:", cancellationToken);

                stopwatch.Stop();
                return new ScanResult(root, volume, raw.FileCount, raw.DirectoryCount, stopwatch.Elapsed);
            },
            cancellationToken);
    }
}
