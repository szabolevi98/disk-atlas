using System.Buffers.Binary;
using System.Text;

namespace DiskAtlas.Ntfs;

/// <summary>
/// The NTFS BIOS parameter block, which sits in the first sector of every NTFS volume
/// and says where the Master File Table starts and how the volume is laid out.
/// </summary>
internal sealed class NtfsBootSector
{
    private const string NtfsOemId = "NTFS    ";

    private NtfsBootSector(
        int bytesPerSector,
        int bytesPerCluster,
        long totalSectors,
        long mftStartLcn,
        int fileRecordSize)
    {
        BytesPerSector = bytesPerSector;
        BytesPerCluster = bytesPerCluster;
        TotalSectors = totalSectors;
        MftStartLcn = mftStartLcn;
        FileRecordSize = fileRecordSize;
    }

    public int BytesPerSector { get; }

    public int BytesPerCluster { get; }

    public long TotalSectors { get; }

    /// <summary>Cluster index where the $MFT itself begins.</summary>
    public long MftStartLcn { get; }

    /// <summary>Size of a single FILE record, almost always 1024 bytes.</summary>
    public int FileRecordSize { get; }

    public long MftStartOffset => MftStartLcn * BytesPerCluster;

    public long VolumeSize => TotalSectors * BytesPerSector;

    public static NtfsBootSector Parse(ReadOnlySpan<byte> sector)
    {
        if (sector.Length < 512)
        {
            throw new InvalidDataException("The boot sector is shorter than 512 bytes.");
        }

        string oemId = Encoding.ASCII.GetString(sector.Slice(0x03, 8));
        if (oemId != NtfsOemId)
        {
            throw new InvalidDataException(
                $"The volume is not NTFS (OEM id is \"{oemId.Trim()}\").");
        }

        int bytesPerSector = BinaryPrimitives.ReadUInt16LittleEndian(sector[0x0B..]);
        if (bytesPerSector is < 256 or > 4096 || !IsPowerOfTwo(bytesPerSector))
        {
            throw new InvalidDataException($"Invalid sector size: {bytesPerSector}.");
        }

        int bytesPerCluster = DecodeClusterSize(sector[0x0D], bytesPerSector);
        long totalSectors = BinaryPrimitives.ReadInt64LittleEndian(sector[0x28..]);
        long mftStartLcn = BinaryPrimitives.ReadInt64LittleEndian(sector[0x30..]);
        int fileRecordSize = DecodeRecordSize((sbyte)sector[0x40], bytesPerCluster);

        return new NtfsBootSector(bytesPerSector, bytesPerCluster, totalSectors, mftStartLcn, fileRecordSize);
    }

    /// <summary>
    /// Sectors per cluster is a byte, but values above 0x80 are stored as a negative
    /// shift so that clusters larger than 64 KB still fit in the field.
    /// </summary>
    private static int DecodeClusterSize(byte sectorsPerCluster, int bytesPerSector)
    {
        int clusterSize = sectorsPerCluster > 0x80
            ? 1 << (256 - sectorsPerCluster)
            : sectorsPerCluster * bytesPerSector;

        if (clusterSize <= 0 || !IsPowerOfTwo(clusterSize))
        {
            throw new InvalidDataException($"Invalid cluster size: {clusterSize}.");
        }

        return clusterSize;
    }

    /// <summary>
    /// Clusters per file record uses the same trick: a negative value is a power of two
    /// in bytes, which is how the usual 1024 byte record fits on a 4096 byte cluster.
    /// </summary>
    private static int DecodeRecordSize(sbyte clustersPerRecord, int bytesPerCluster)
    {
        int recordSize = clustersPerRecord < 0
            ? 1 << -clustersPerRecord
            : clustersPerRecord * bytesPerCluster;

        if (recordSize < 256 || !IsPowerOfTwo(recordSize))
        {
            throw new InvalidDataException($"Invalid file record size: {recordSize}.");
        }

        return recordSize;
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;
}
