using System.Buffers.Binary;
using System.Text;
using DiskAtlas.Ntfs;

int failures = 0;

void Check(string name, bool condition, string detail = "")
{
    if (condition)
    {
        Console.WriteLine($"PASS  {name}");
    }
    else
    {
        failures++;
        Console.WriteLine($"FAIL  {name}  {detail}");
    }
}

// ---------------------------------------------------------------- boot sector

byte[] boot = new byte[512];
Encoding.ASCII.GetBytes("NTFS    ").CopyTo(boot, 0x03);
BinaryPrimitives.WriteUInt16LittleEndian(boot.AsSpan(0x0B), 512);   // bytes per sector
boot[0x0D] = 8;                                                      // 8 sectors per cluster
BinaryPrimitives.WriteInt64LittleEndian(boot.AsSpan(0x28), 1_000_000);
BinaryPrimitives.WriteInt64LittleEndian(boot.AsSpan(0x30), 786_432); // MFT start cluster
boot[0x40] = unchecked((byte)(sbyte)-10);                            // 1 << 10 = 1024 byte records

NtfsBootSector parsed = NtfsBootSector.Parse(boot);
Check("boot: sector size", parsed.BytesPerSector == 512, $"got {parsed.BytesPerSector}");
Check("boot: cluster size", parsed.BytesPerCluster == 4096, $"got {parsed.BytesPerCluster}");
Check("boot: record size", parsed.FileRecordSize == 1024, $"got {parsed.FileRecordSize}");
Check("boot: mft offset", parsed.MftStartOffset == 786_432L * 4096, $"got {parsed.MftStartOffset}");
Check("boot: volume size", parsed.VolumeSize == 1_000_000L * 512, $"got {parsed.VolumeSize}");

// A cluster larger than 64 KB is encoded as a negative shift instead of a sector count.
byte[] bigCluster = (byte[])boot.Clone();
bigCluster[0x0D] = 0xF1;                                             // 1 << (256 - 241) = 32768
Check("boot: oversized cluster encoding", NtfsBootSector.Parse(bigCluster).BytesPerCluster == 32768,
    $"got {NtfsBootSector.Parse(bigCluster).BytesPerCluster}");

byte[] notNtfs = (byte[])boot.Clone();
Encoding.ASCII.GetBytes("FAT32   ").CopyTo(notNtfs, 0x03);
bool rejected = false;
try { NtfsBootSector.Parse(notNtfs); } catch (InvalidDataException) { rejected = true; }
Check("boot: rejects non NTFS", rejected);

// ------------------------------------------------------------------ data runs

// 0x21 -> 1 length byte, 2 offset bytes. Length 0x18 = 24, offset 0x5634 = 22068.
List<DataRun> single = DataRun.Decode(new byte[] { 0x21, 0x18, 0x34, 0x56, 0x00 });
Check("runs: single run", single.Count == 1 && single[0].StartLcn == 22068 && single[0].ClusterCount == 24,
    single.Count > 0 ? $"got lcn {single[0].StartLcn}, count {single[0].ClusterCount}" : "no runs");

// The second offset is relative to the first, which is what keeps run lists compact.
List<DataRun> chained = DataRun.Decode(new byte[] { 0x21, 0x18, 0x34, 0x56, 0x21, 0x0A, 0x00, 0x01, 0x00 });
Check("runs: relative offset", chained.Count == 2 && chained[1].StartLcn == 22068 + 256 && chained[1].ClusterCount == 10,
    chained.Count > 1 ? $"got lcn {chained[1].StartLcn}" : "missing second run");

// A negative offset moves backwards on the volume.
List<DataRun> backwards = DataRun.Decode(new byte[] { 0x21, 0x18, 0x34, 0x56, 0x21, 0x0A, 0x00, 0xFF, 0x00 });
Check("runs: negative offset", backwards.Count == 2 && backwards[1].StartLcn == 22068 - 256,
    backwards.Count > 1 ? $"got lcn {backwards[1].StartLcn}" : "missing second run");

// A run with no offset field is a sparse hole and occupies nothing.
List<DataRun> sparse = DataRun.Decode(new byte[] { 0x01, 0x10, 0x21, 0x18, 0x34, 0x56, 0x00 });
Check("runs: sparse hole skipped", sparse.Count == 1 && sparse[0].ClusterCount == 24,
    $"got {sparse.Count} runs");

Check("runs: empty list", DataRun.Decode(new byte[] { 0x00 }).Count == 0);

// ----------------------------------------------------------------- FILE record

byte[] record = BuildFileRecord(
    name: "holiday-video.mkv",
    parentRecord: 1234,
    realSize: 8_589_934_592,      // 8 GB
    allocatedSize: 8_589_938_688,
    isDirectory: false,
    compressed: false);

Check("record: parses", FileRecordParser.TryParse(record, 512, out FileRecord file));
Check("record: in use", file.InUse);
Check("record: not a directory", !file.IsDirectory);
Check("record: name", file.Name == "holiday-video.mkv", $"got \"{file.Name}\"");
Check("record: parent", file.ParentRecordNumber == 1234, $"got {file.ParentRecordNumber}");
Check("record: logical size", file.RealSize == 8_589_934_592, $"got {file.RealSize}");
Check("record: size on disk", file.AllocatedSize == 8_589_938_688, $"got {file.AllocatedSize}");
Check("record: not an extension", !file.IsExtensionRecord);

// A torn or stale read leaves a sector tail that does not match the update sequence
// number, and the record must be refused rather than silently misparsed.
byte[] torn = BuildFileRecord("torn.bin", 7, 100, 4096, false, false);
torn[510] = 0xAB;
Check("record: rejects broken fixup", !FileRecordParser.TryParse(torn, 512, out _));

// The fixup really does hide the true bytes: without restoring them the size would be
// read from a sector tail that NTFS overwrote.
byte[] fixupProof = BuildFileRecord("fixup.bin", 7, 123_456, 126_976, false, false);
Check("record: fixup restores sector tail",
    FileRecordParser.TryParse(fixupProof, 512, out FileRecord restored) && restored.RealSize == 123_456,
    $"got {restored.RealSize}");

byte[] directory = BuildFileRecord("Windows", 5, 0, 0, isDirectory: true, compressed: false);
Check("record: directory flag", FileRecordParser.TryParse(directory, 512, out FileRecord dir) && dir.IsDirectory);
Check("record: directory name", dir.Name == "Windows", $"got \"{dir.Name}\"");

byte[] compressed = BuildFileRecord("archive.log", 9, 1_000_000, 250_000, false, compressed: true);
Check("record: compressed flag",
    FileRecordParser.TryParse(compressed, 512, out FileRecord comp) && comp.IsCompressed && comp.AllocatedSize == 250_000,
    $"got compressed={comp.IsCompressed}, allocated={comp.AllocatedSize}");

byte[] garbage = new byte[1024];
Check("record: rejects garbage", !FileRecordParser.TryParse(garbage, 512, out _));

// The $MFT describes its own extents through the run list of its $DATA attribute.
byte[] mftRecord = BuildFileRecord("$MFT", 5, 262_144, 262_144, false, false, dataRuns: [0x21, 0x18, 0x34, 0x56, 0x00]);
List<DataRun>? mftRuns = FileRecordParser.ReadDataRuns(mftRecord, 512);
Check("record: reads own run list",
    mftRuns is { Count: 1 } && mftRuns[0].StartLcn == 22068 && mftRuns[0].ClusterCount == 24,
    mftRuns is null ? "no runs" : $"got {mftRuns.Count} runs");

Console.WriteLine();
Console.WriteLine(failures == 0 ? "all checks passed" : $"{failures} check(s) failed");
return failures == 0 ? 0 : 1;

// Builds a 1024 byte FILE record with two sectors, a $FILE_NAME and a non resident
// $DATA attribute, including the update sequence fixups NTFS applies on disk.
static byte[] BuildFileRecord(
    string name,
    long parentRecord,
    long realSize,
    long allocatedSize,
    bool isDirectory,
    bool compressed,
    byte[]? dataRuns = null)
{
    const int recordSize = 1024;
    const int sectorSize = 512;
    const int usaOffset = 0x30;
    const ushort updateSequenceNumber = 0x0007;

    byte[] record = new byte[recordSize];
    Encoding.ASCII.GetBytes("FILE").CopyTo(record, 0);
    BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(0x04), usaOffset);
    BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(0x06), 3);   // USN plus one entry per sector
    BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(0x12), 1);   // hard link count
    BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(0x14), 0x38);
    BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(0x16), (ushort)(isDirectory ? 0x0003 : 0x0001));
    BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(0x1C), recordSize);

    int offset = 0x38;

    // $FILE_NAME
    byte[] nameBytes = Encoding.Unicode.GetBytes(name);
    int fileNameValue = 0x42 + nameBytes.Length;
    int fileNameLength = Align8(0x18 + fileNameValue);
    Span<byte> fileName = record.AsSpan(offset, fileNameLength);
    BinaryPrimitives.WriteUInt32LittleEndian(fileName, 0x30);
    BinaryPrimitives.WriteUInt32LittleEndian(fileName[0x04..], (uint)fileNameLength);
    BinaryPrimitives.WriteUInt32LittleEndian(fileName[0x10..], (uint)fileNameValue);
    BinaryPrimitives.WriteUInt16LittleEndian(fileName[0x14..], 0x18);
    Span<byte> value = fileName.Slice(0x18, fileNameValue);
    BinaryPrimitives.WriteInt64LittleEndian(value, parentRecord);
    value[0x40] = (byte)name.Length;
    value[0x41] = 1;                                                   // Win32 namespace
    nameBytes.CopyTo(value[0x42..]);
    offset += fileNameLength;

    // $DATA, always non resident here so allocated and real sizes are both present.
    byte[] runs = dataRuns ?? [0x21, 0x08, 0x00, 0x10, 0x00];
    int dataLength = Align8(0x40 + runs.Length);
    Span<byte> data = record.AsSpan(offset, dataLength);
    BinaryPrimitives.WriteUInt32LittleEndian(data, 0x80);
    BinaryPrimitives.WriteUInt32LittleEndian(data[0x04..], (uint)dataLength);
    data[0x08] = 1;                                                    // non resident
    BinaryPrimitives.WriteUInt16LittleEndian(data[0x0C..], (ushort)(compressed ? 0x0001 : 0x0000));
    BinaryPrimitives.WriteUInt16LittleEndian(data[0x20..], 0x40);      // run list offset
    BinaryPrimitives.WriteInt64LittleEndian(data[0x28..], allocatedSize);
    BinaryPrimitives.WriteInt64LittleEndian(data[0x30..], realSize);
    BinaryPrimitives.WriteInt64LittleEndian(data[0x38..], realSize);
    runs.CopyTo(data[0x40..]);
    offset += dataLength;

    BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(offset), 0xFFFFFFFF);
    offset += 4;
    BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(0x18), (uint)offset);

    // Move the real sector tails into the update sequence array and stamp the USN over
    // them, exactly as NTFS does when it writes the record.
    BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(usaOffset), updateSequenceNumber);
    for (int sector = 1; sector <= recordSize / sectorSize; sector++)
    {
        int tail = (sector * sectorSize) - 2;
        record.AsSpan(tail, 2).CopyTo(record.AsSpan(usaOffset + (sector * 2), 2));
        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(tail), updateSequenceNumber);
    }

    return record;
}

static int Align8(int length) => (length + 7) & ~7;
