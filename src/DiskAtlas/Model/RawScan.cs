using DiskAtlas.Ntfs;

namespace DiskAtlas.Model;

/// <summary>How far a running scan has progressed.</summary>
internal readonly record struct ScanProgress(long RecordsRead, long TotalRecords)
{
    public double Fraction => TotalRecords <= 0 ? 0 : Math.Clamp((double)RecordsRead / TotalRecords, 0, 1);
}

/// <summary>One usable FILE record, flattened into a value type.</summary>
internal struct RawEntry
{
    public string? Name;
    public long ParentRecordNumber;
    public long LogicalSize;
    public long SizeOnDisk;
    public int HardLinkCount;
    public bool IsDirectory;
    public bool IsCompressed;
    public bool IsSparse;
    public bool Present;
}

/// <summary>
/// The flat result of reading the table: entries indexed by their record number, which is
/// also how parents are referenced. Nothing is linked up yet.
/// </summary>
internal sealed class RawScan
{
    private readonly RawEntry[] _entries;

    public RawScan(long totalRecords, int bytesPerCluster, long volumeSize)
    {
        int capacity = (int)Math.Clamp(totalRecords + 1, 1, int.MaxValue);
        _entries = new RawEntry[capacity];
        BytesPerCluster = bytesPerCluster;
        VolumeSize = volumeSize;
    }

    public int BytesPerCluster { get; }

    public long VolumeSize { get; }

    public int Capacity => _entries.Length;

    public int FileCount { get; private set; }

    public int DirectoryCount { get; private set; }

    public ref RawEntry this[long recordNumber] => ref _entries[recordNumber];

    public bool IsValidRecord(long recordNumber) =>
        recordNumber >= 0 && recordNumber < _entries.Length && _entries[recordNumber].Present;

    public void Add(long recordNumber, FileRecord record)
    {
        if (recordNumber < 0 || recordNumber >= _entries.Length)
        {
            return;
        }

        ref RawEntry entry = ref _entries[recordNumber];
        entry.Present = true;
        entry.Name = record.Name;
        entry.ParentRecordNumber = record.ParentRecordNumber;
        entry.IsDirectory = record.IsDirectory;
        entry.IsCompressed = record.IsCompressed;
        entry.IsSparse = record.IsSparse;
        entry.HardLinkCount = record.HardLinkCount;

        if (record.IsDirectory)
        {
            DirectoryCount++;
            return;
        }

        FileCount++;
        entry.LogicalSize = Math.Max(0, record.RealSize);

        // A resident file lives inside its own FILE record and occupies no clusters of its
        // own, so it costs nothing beyond the table that was already counted.
        entry.SizeOnDisk = Math.Max(0, record.AllocatedSize);
    }
}
