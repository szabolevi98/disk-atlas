using System.Buffers.Binary;
using System.Text;

namespace DiskAtlas.Ntfs;

/// <summary>Attribute type codes used while walking a FILE record.</summary>
internal static class AttributeType
{
    public const uint StandardInformation = 0x10;
    public const uint AttributeList = 0x20;
    public const uint FileName = 0x30;
    public const uint Data = 0x80;
    public const uint EndMarker = 0xFFFFFFFF;
}

/// <summary>
/// The interesting parts of a single FILE record: what it is called, which directory it
/// lives in and how much space it takes.
/// </summary>
internal struct FileRecord
{
    public bool InUse;
    public bool IsDirectory;
    public bool IsCompressed;
    public bool IsSparse;

    /// <summary>Record number of the parent directory, taken from $FILE_NAME.</summary>
    public long ParentRecordNumber;

    /// <summary>Number of directory entries pointing at this record.</summary>
    public int HardLinkCount;

    /// <summary>Logical file size from the unnamed $DATA attribute.</summary>
    public long RealSize;

    /// <summary>Space actually occupied on the volume, which differs when compressed.</summary>
    public long AllocatedSize;

    public string? Name;

    /// <summary>True when the record is an extension of another record and should be skipped.</summary>
    public bool IsExtensionRecord;
}

internal static class FileRecordParser
{
    private static readonly uint FileSignature = BinaryPrimitives.ReadUInt32LittleEndian("FILE"u8);

    private const ushort FlagInUse = 0x0001;
    private const ushort FlagDirectory = 0x0002;

    private const ushort AttributeCompressed = 0x0001;
    private const ushort AttributeSparse = 0x8000;

    // $FILE_NAME namespaces. A record often carries both a long name and a legacy 8.3
    // name; the DOS-only one is never what the user sees.
    private const byte NamespaceDos = 2;

    /// <summary>
    /// Parses one FILE record in place. The buffer is modified because update sequence
    /// fixups have to be applied before any field can be trusted.
    /// </summary>
    /// <returns>False when the buffer does not hold a usable record.</returns>
    public static bool TryParse(Span<byte> record, int bytesPerSector, out FileRecord result)
    {
        result = default;

        if (record.Length < 48 || BinaryPrimitives.ReadUInt32LittleEndian(record) != FileSignature)
        {
            return false;
        }

        if (!ApplyFixups(record, bytesPerSector))
        {
            return false;
        }

        ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(record[0x16..]);
        result.InUse = (flags & FlagInUse) != 0;
        result.IsDirectory = (flags & FlagDirectory) != 0;
        result.HardLinkCount = BinaryPrimitives.ReadUInt16LittleEndian(record[0x12..]);
        result.ParentRecordNumber = -1;

        // A non zero base record means this is a continuation of another record. Its
        // attributes belong to the base, so counting it again would double the size.
        long baseRecord = BinaryPrimitives.ReadInt64LittleEndian(record[0x20..]) & 0x0000FFFFFFFFFFFF;
        result.IsExtensionRecord = baseRecord != 0;

        if (!result.InUse)
        {
            return true;
        }

        int usedSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(record[0x18..]);
        int limit = Math.Clamp(usedSize, 0, record.Length);
        int offset = BinaryPrimitives.ReadUInt16LittleEndian(record[0x14..]);

        byte bestNamespace = byte.MaxValue;
        bool dataSeen = false;

        while (offset >= 0 && offset + 8 <= limit)
        {
            uint type = BinaryPrimitives.ReadUInt32LittleEndian(record[offset..]);
            if (type == AttributeType.EndMarker)
            {
                break;
            }

            int length = (int)BinaryPrimitives.ReadUInt32LittleEndian(record[(offset + 4)..]);
            if (length <= 0 || offset + length > limit)
            {
                break;
            }

            Span<byte> attribute = record.Slice(offset, length);

            switch (type)
            {
                case AttributeType.FileName:
                    ReadFileName(attribute, ref result, ref bestNamespace);
                    break;

                case AttributeType.Data:
                    ReadData(attribute, ref result, ref dataSeen);
                    break;
            }

            offset += length;
        }

        return true;
    }

    /// <summary>
    /// NTFS overwrites the last two bytes of every sector in a record with an update
    /// sequence number so torn writes can be detected. The real values live in the update
    /// sequence array and must be put back before the record is readable.
    /// </summary>
    private static bool ApplyFixups(Span<byte> record, int bytesPerSector)
    {
        int arrayOffset = BinaryPrimitives.ReadUInt16LittleEndian(record[0x04..]);
        int arrayCount = BinaryPrimitives.ReadUInt16LittleEndian(record[0x06..]);

        if (arrayCount == 0 || arrayOffset + (arrayCount * 2) > record.Length)
        {
            return false;
        }

        ushort expected = BinaryPrimitives.ReadUInt16LittleEndian(record[arrayOffset..]);

        // The first array entry is the signature itself, so the fixups start at index 1.
        for (int i = 1; i < arrayCount; i++)
        {
            int sectorEnd = (i * bytesPerSector) - 2;
            if (sectorEnd + 2 > record.Length)
            {
                return false;
            }

            Span<byte> tail = record.Slice(sectorEnd, 2);
            if (BinaryPrimitives.ReadUInt16LittleEndian(tail) != expected)
            {
                // The sector does not belong to this record: the volume changed underneath
                // us, or the read was torn.
                return false;
            }

            record.Slice(arrayOffset + (i * 2), 2).CopyTo(tail);
        }

        return true;
    }

    private static void ReadFileName(ReadOnlySpan<byte> attribute, ref FileRecord result, ref byte bestNamespace)
    {
        // $FILE_NAME is always resident.
        if (attribute[0x08] != 0)
        {
            return;
        }

        int valueOffset = BinaryPrimitives.ReadUInt16LittleEndian(attribute[0x14..]);
        int valueLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(attribute[0x10..]);
        if (valueOffset + valueLength > attribute.Length || valueLength < 0x42)
        {
            return;
        }

        ReadOnlySpan<byte> value = attribute.Slice(valueOffset, valueLength);

        byte nameSpace = value[0x41];
        int nameLength = value[0x40] * 2;
        if (0x42 + nameLength > value.Length)
        {
            return;
        }

        // Prefer the Win32 name over the generated 8.3 alias.
        if (result.Name is not null && (nameSpace == NamespaceDos || nameSpace >= bestNamespace))
        {
            return;
        }

        bestNamespace = nameSpace;
        result.ParentRecordNumber = BinaryPrimitives.ReadInt64LittleEndian(value) & 0x0000FFFFFFFFFFFF;
        result.Name = Encoding.Unicode.GetString(value.Slice(0x42, nameLength));
    }

    private static void ReadData(ReadOnlySpan<byte> attribute, ref FileRecord result, ref bool dataSeen)
    {
        // Named streams are alternate data streams; only the unnamed one is the file.
        if (attribute[0x09] != 0 || dataSeen)
        {
            return;
        }

        dataSeen = true;

        ushort attributeFlags = BinaryPrimitives.ReadUInt16LittleEndian(attribute[0x0C..]);
        result.IsCompressed = (attributeFlags & AttributeCompressed) != 0;
        result.IsSparse = (attributeFlags & AttributeSparse) != 0;

        if (attribute[0x08] == 0)
        {
            // Resident: a small file stored inside the record itself.
            long residentSize = BinaryPrimitives.ReadUInt32LittleEndian(attribute[0x10..]);
            result.RealSize = residentSize;
            result.AllocatedSize = 0;
            return;
        }

        if (attribute.Length < 0x38)
        {
            return;
        }

        result.AllocatedSize = BinaryPrimitives.ReadInt64LittleEndian(attribute[0x28..]);
        result.RealSize = BinaryPrimitives.ReadInt64LittleEndian(attribute[0x30..]);
    }

    /// <summary>
    /// Reads the run list of the first non resident unnamed $DATA attribute, which is how
    /// the $MFT describes where its own extents live.
    /// </summary>
    public static List<DataRun>? ReadDataRuns(Span<byte> record, int bytesPerSector)
    {
        if (record.Length < 48 || BinaryPrimitives.ReadUInt32LittleEndian(record) != FileSignature)
        {
            return null;
        }

        if (!ApplyFixups(record, bytesPerSector))
        {
            return null;
        }

        int usedSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(record[0x18..]);
        int limit = Math.Clamp(usedSize, 0, record.Length);
        int offset = BinaryPrimitives.ReadUInt16LittleEndian(record[0x14..]);

        while (offset >= 0 && offset + 8 <= limit)
        {
            uint type = BinaryPrimitives.ReadUInt32LittleEndian(record[offset..]);
            if (type == AttributeType.EndMarker)
            {
                break;
            }

            int length = (int)BinaryPrimitives.ReadUInt32LittleEndian(record[(offset + 4)..]);
            if (length <= 0 || offset + length > limit)
            {
                break;
            }

            Span<byte> attribute = record.Slice(offset, length);
            bool unnamed = attribute[0x09] == 0;
            bool nonResident = attribute[0x08] != 0;

            if (type == AttributeType.Data && unnamed && nonResident)
            {
                int runsOffset = BinaryPrimitives.ReadUInt16LittleEndian(attribute[0x20..]);
                if (runsOffset > 0 && runsOffset < attribute.Length)
                {
                    return DataRun.Decode(attribute[runsOffset..]);
                }
            }

            offset += length;
        }

        return null;
    }
}
