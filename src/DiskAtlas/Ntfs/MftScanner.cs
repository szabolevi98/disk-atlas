using DiskAtlas.Model;

namespace DiskAtlas.Ntfs;

/// <summary>
/// Walks every FILE record of a volume by reading the $MFT extents in order. This is the
/// reason a full volume can be mapped in seconds: the table is one mostly contiguous
/// stream, while a directory walk costs a seek per folder.
/// </summary>
internal sealed class MftScanner
{
    /// <summary>How much of the table to pull in one read. Larger blocks amortise the IO.</summary>
    private const int ReadBlockSize = 4 * 1024 * 1024;

    /// <summary>Record number 5 is always the volume root directory.</summary>
    public const long RootRecordNumber = 5;

    private readonly string _driveLetter;

    public MftScanner(string driveLetter)
    {
        _driveLetter = driveLetter;
    }

    public RawScan Scan(IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        using var reader = new VolumeReader(_driveLetter);

        byte[] bootSector = new byte[512];
        reader.ReadExact(0, bootSector);
        NtfsBootSector boot = NtfsBootSector.Parse(bootSector);
        reader.BytesPerSector = boot.BytesPerSector;

        List<DataRun> runs = ReadMftRuns(reader, boot);
        long totalRecords = runs.Sum(run => run.ClusterCount) * boot.BytesPerCluster / boot.FileRecordSize;

        var scan = new RawScan(totalRecords, boot.BytesPerCluster, boot.VolumeSize);

        int recordsPerBlock = Math.Max(1, ReadBlockSize / boot.FileRecordSize);
        int blockSize = recordsPerBlock * boot.FileRecordSize;
        byte[] block = new byte[blockSize];

        long recordNumber = 0;
        long lastReported = 0;

        foreach (DataRun run in runs)
        {
            long runBytes = run.ClusterCount * boot.BytesPerCluster;
            long runOffset = run.StartLcn * boot.BytesPerCluster;

            for (long consumed = 0; consumed < runBytes; consumed += blockSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int length = (int)Math.Min(blockSize, runBytes - consumed);
                Span<byte> window = block.AsSpan(0, length);
                reader.ReadExact(runOffset + consumed, window);

                for (int position = 0; position + boot.FileRecordSize <= length; position += boot.FileRecordSize)
                {
                    Span<byte> recordBytes = window.Slice(position, boot.FileRecordSize);

                    if (FileRecordParser.TryParse(recordBytes, boot.BytesPerSector, out FileRecord record)
                        && record.InUse
                        && !record.IsExtensionRecord
                        && record.Name is not null)
                    {
                        scan.Add(recordNumber, record);
                    }

                    recordNumber++;
                }

                if (progress is not null && recordNumber - lastReported >= 50_000)
                {
                    lastReported = recordNumber;
                    progress.Report(new ScanProgress(recordNumber, totalRecords));
                }
            }
        }

        progress?.Report(new ScanProgress(totalRecords, totalRecords));
        return scan;
    }

    /// <summary>
    /// The $MFT describes its own location: record zero of the table holds a $DATA
    /// attribute whose run list points at every extent of the table itself.
    /// </summary>
    private static List<DataRun> ReadMftRuns(VolumeReader reader, NtfsBootSector boot)
    {
        byte[] firstRecord = new byte[boot.FileRecordSize];
        reader.ReadExact(boot.MftStartOffset, firstRecord);

        List<DataRun>? runs = FileRecordParser.ReadDataRuns(firstRecord, boot.BytesPerSector);

        if (runs is null || runs.Count == 0)
        {
            throw new InvalidDataException("Could not read the run list of the Master File Table.");
        }

        return runs;
    }
}
