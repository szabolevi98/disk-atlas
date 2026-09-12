using System.Buffers.Binary;
using System.Text;
using System.Drawing;
using DiskAtlas;
using DiskAtlas.Model;
using DiskAtlas.Ntfs;
using DiskAtlas.Rendering;

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

// ------------------------------------------------------------------- treemap

// A synthetic tree with known proportions: every rectangle must land inside the canvas,
// the whole canvas must be covered, and the areas must follow the sizes.
DiskNode sample = BuildSampleTree();
using (TreemapRender render = TreemapRenderer.Render(sample, 640, 400))
{
    Check("treemap: produced rectangles", render.Items.Count > 0, $"got {render.Items.Count}");

    bool inside = render.Items.All(item =>
        item.Bounds.Left >= 0 && item.Bounds.Top >= 0 &&
        item.Bounds.Right <= 640 && item.Bounds.Bottom <= 400);
    Check("treemap: rectangles stay on the canvas", inside);

    int covered = 0;
    for (int y = 0; y < 400; y++)
    {
        for (int x = 0; x < 640; x++)
        {
            if (render.HitTest(x, y) is not null) covered++;
        }
    }
    double coverage = covered / (640.0 * 400.0);
    Check("treemap: covers the canvas", coverage > 0.97, $"covered {coverage:P1}");

    // The biggest child holds half the volume, so it must occupy about half the pixels.
    DiskNode biggest = sample.Children[0];
    int biggestPixels = 0;
    for (int y = 0; y < 400; y++)
    {
        for (int x = 0; x < 640; x++)
        {
            DiskNode? hit = render.HitTest(x, y)?.Node;
            for (DiskNode? walk = hit; walk is not null; walk = walk.Parent)
            {
                if (ReferenceEquals(walk, biggest)) { biggestPixels++; break; }
            }
        }
    }
    double share = biggestPixels / (640.0 * 400.0);
    Check("treemap: areas follow sizes", Math.Abs(share - 0.5) < 0.03, $"got {share:P1}, expected about 50%");

    // Cushion shading means a flat fill would be a bug: the same file type must show a
    // range of brightness rather than one colour.
    var distinct = new HashSet<int>();
    for (int x = 0; x < 640; x += 3) distinct.Add(render.Image.GetPixel(x, 200).ToArgb());
    Check("treemap: cushion shading varies", distinct.Count > 20, $"only {distinct.Count} distinct colours");

    string preview = Path.Combine(AppContext.BaseDirectory, "treemap-preview.png");
    render.Image.Save(preview, System.Drawing.Imaging.ImageFormat.Png);
    Console.WriteLine($"      preview written to {preview}");
}

static DiskNode BuildSampleTree()
{
    DiskNode root = new("C:", true);

    // A realistic folder has a few big files and a long tail of small ones, which is what
    // makes the map look like a mosaic rather than a handful of slabs.
    static (string Name, long Size)[] Spread(string extension, int count, long total)
    {
        double[] weights = new double[count];
        double sum = 0;
        for (int i = 0; i < count; i++)
        {
            weights[i] = 1.0 / Math.Pow(i + 1, 1.35);
            sum += weights[i];
        }

        var files = new (string, long)[count];
        for (int i = 0; i < count; i++)
        {
            files[i] = ($"file{i:D3}.{extension}", Math.Max(1, (long)(total * weights[i] / sum * 1000)));
        }

        return files;
    }

    void Add(DiskNode parent, string name, long size, params (string Name, long Size)[] children)
    {
        long folderSize = 0;
        foreach ((string _, long childSize) in children) folderSize += childSize;
        DiskNode folder = new(name, true) { SizeOnDisk = folderSize, LogicalSize = folderSize, FileCount = children.Length };
        foreach ((string childName, long childSize) in children)
        {
            folder.AddChild(new DiskNode(childName, false)
            {
                SizeOnDisk = childSize,
                LogicalSize = childSize,
                FileCount = 1,
            });
        }
        parent.AddChild(folder);
    }

    Add(root, "Videos", 500, Spread("mkv", 18, 500));
    Add(root, "Games", 250, Spread("pak", 40, 250));
    Add(root, "Windows", 150, Spread("dll", 90, 150));
    Add(root, "Photos", 60, Spread("jpg", 70, 60));
    Add(root, "Source", 40, Spread("cs", 55, 40));

    foreach (DiskNode child in root.Children) { root.SizeOnDisk += child.SizeOnDisk; root.FileCount += child.FileCount; }
    root.LogicalSize = root.SizeOnDisk;
    return root;
}

// A folder holding tens of thousands of files is the case that used to freeze the
// window. The map must still build quickly, and the selection region must be cheap
// enough that it can be recomputed whenever the selection changes.
DiskNode crowded = BuildCrowdedTree(60_000);
var renderWatch = System.Diagnostics.Stopwatch.StartNew();
using (TreemapRender big = TreemapRenderer.Render(crowded, 1200, 700))
{
    renderWatch.Stop();
    Check("treemap: a crowded folder renders quickly",
        renderWatch.ElapsedMilliseconds < 2000,
        $"took {renderWatch.ElapsedMilliseconds} ms");

    // This is the pass that used to run on every single repaint, including each mouse
    // move, once anything was selected.
    DiskNode target = crowded.Children[0];
    var outlineWatch = System.Diagnostics.Stopwatch.StartNew();
    Rectangle? union = null;
    foreach (TreemapItem item in big.Items)
    {
        for (DiskNode? walk = item.Node; walk is not null; walk = walk.Parent)
        {
            if (!ReferenceEquals(walk, target)) continue;
            union = union is null ? item.Bounds : Rectangle.Union(union.Value, item.Bounds);
            break;
        }
    }
    outlineWatch.Stop();

    Check("treemap: the selection region can be found", union is not null);
    Console.WriteLine($"      {big.Items.Count:N0} rectangles, map built in {renderWatch.ElapsedMilliseconds} ms, "
        + $"selection region scanned in {outlineWatch.Elapsed.TotalMilliseconds:N1} ms");
    Console.WriteLine($"      that scan ran on every repaint before it was cached");
}

static DiskNode BuildCrowdedTree(int fileCount)
{
    DiskNode root = new("C:", true);
    DiskNode folder = new("WinSxS", true);

    long total = 0;
    for (int i = 0; i < fileCount; i++)
    {
        long size = 4096 + ((i * 7919) % 262_144);
        folder.AddChild(new DiskNode($"component{i:D6}.manifest", false)
        {
            SizeOnDisk = size,
            LogicalSize = size,
            FileCount = 1,
        });
        total += size;
    }

    folder.SizeOnDisk = total;
    folder.LogicalSize = total;
    folder.FileCount = fileCount;
    folder.SortChildrenBySize();

    root.AddChild(folder);
    root.SizeOnDisk = total;
    root.LogicalSize = total;
    root.FileCount = fileCount;
    return root;
}

// ---------------------------------------------------------------- tree building

// NTFS calls the root directory ".", so the tree has to put the volume there instead,
// otherwise every path starts with a full stop.
var scan = new RawScan(16, 4096, 1_000_000);
scan.Add(5, new FileRecord { InUse = true, IsDirectory = true, Name = ".", ParentRecordNumber = 5 });
scan.Add(6, new FileRecord { InUse = true, IsDirectory = true, Name = "Windows", ParentRecordNumber = 5 });
scan.Add(7, new FileRecord
{
    InUse = true,
    IsDirectory = false,
    Name = "notepad.exe",
    ParentRecordNumber = 6,
    RealSize = 200_000,
    AllocatedSize = 204_800,
});

DiskNode built = DiskAtlas.Scanning.TreeBuilder.Build(scan, "D:", CancellationToken.None);
Check("tree: root carries the volume, not a full stop", built.Name == "D:", $"got \"{built.Name}\"");
Check("tree: sizes roll up", built.SizeOnDisk == 204_800, $"got {built.SizeOnDisk}");

DiskNode? windows = built.Children.FirstOrDefault(child => child.Name == "Windows");
Check("tree: children are attached to their parent", windows is not null);
Check("tree: paths start at the volume",
    windows?.Children.FirstOrDefault()?.FullPath == @"D:\Windows\notepad.exe",
    $"got \"{windows?.Children.FirstOrDefault()?.FullPath}\"");

// ---------------------------------------------------------------- window sizing

// The designer size, and the smallest the window may become.
var designer = new Size(1356, 819);
var minimum = new Size(900, 560);

// A desktop monitor has room to spare, so nothing should change.
var desktop = new Rectangle(0, 0, 1920, 1040);
Check("sizing: a roomy screen is left alone",
    WindowSizing.FitWithin(designer, minimum, desktop) == designer,
    $"got {WindowSizing.FitWithin(designer, minimum, desktop)}");

// The laptop this was raised for: 1366 by 768 with a task bar.
var laptop = new Rectangle(0, 0, 1366, 728);
Size fitted = WindowSizing.FitWithin(designer, minimum, laptop);
Check("sizing: the laptop keeps the window on screen",
    fitted.Width <= laptop.Width && fitted.Height <= laptop.Height,
    $"got {fitted} inside {laptop.Width}x{laptop.Height}");
Check("sizing: the laptop keeps the table unscrolled",
    fitted.Width >= 1272,
    $"got {fitted.Width}, the six columns plus the folder pane need about 1272");
Console.WriteLine($"      1366x768 laptop opens at {fitted.Width}x{fitted.Height}");

// Something genuinely small must not shrink past the point of being usable.
var tiny = new Rectangle(0, 0, 800, 600);
Size floor = WindowSizing.FitWithin(designer, minimum, tiny);
Check("sizing: never smaller than the minimum",
    floor.Width >= minimum.Width && floor.Height >= minimum.Height,
    $"got {floor}");

// Centring has to account for a task bar that does not sit at the bottom.
var offset = new Rectangle(60, 0, 1860, 1040);
Point where = WindowSizing.CenterWithin(new Size(1000, 800), offset);
Check("sizing: centred inside the working area, not the screen",
    where.X == 60 + 430 && where.Y == 120,
    $"got {where}");

// ------------------------------------------------------------------- deleting

// Removing something from disk has to take its figures out of every folder above it,
// otherwise the totals and the map keep describing a disk that no longer exists.
DiskNode volume = new("C:", true);
DiskNode games = new("Games", true);
DiskNode assets = new("Assets", true);

DiskNode pak = new("pak0.pak", false) { SizeOnDisk = 600, LogicalSize = 590, FileCount = 1 };
DiskNode texture = new("textures.bin", false) { SizeOnDisk = 300, LogicalSize = 300, FileCount = 1 };
DiskNode readme = new("readme.txt", false) { SizeOnDisk = 100, LogicalSize = 40, FileCount = 1 };

assets.AddChild(pak);
assets.AddChild(texture);
assets.SizeOnDisk = 900; assets.LogicalSize = 890; assets.FileCount = 2;

games.AddChild(assets);
games.AddChild(readme);
games.SizeOnDisk = 1000; games.LogicalSize = 930; games.FileCount = 3;

volume.AddChild(games);
volume.SizeOnDisk = 1000; volume.LogicalSize = 930; volume.FileCount = 3;

Check("delete: a file leaves its parent", readme.Detach());
Check("delete: the parent loses the size", games.SizeOnDisk == 900, $"got {games.SizeOnDisk}");
Check("delete: the volume loses it too", volume.SizeOnDisk == 900, $"got {volume.SizeOnDisk}");
Check("delete: the file count follows", volume.FileCount == 2, $"got {volume.FileCount}");
Check("delete: the logical size follows", volume.LogicalSize == 890, $"got {volume.LogicalSize}");
Check("delete: the row is gone from the parent",
    !games.Children.Any(child => ReferenceEquals(child, readme)));

// A folder takes everything under it, counted once at the folder rather than per file.
Check("delete: a folder leaves its parent", assets.Detach());
Check("delete: the subtree is subtracted", volume.SizeOnDisk == 0, $"got {volume.SizeOnDisk}");
Check("delete: the files inside are subtracted", volume.FileCount == 0, $"got {volume.FileCount}");
Check("delete: the detached folder keeps its own figures", assets.SizeOnDisk == 900,
    $"got {assets.SizeOnDisk}");
Check("delete: the detached folder has no parent", assets.Parent is null);

// The volume root has nothing above it, so it cannot be removed this way.
Check("delete: the volume root refuses", !volume.Detach());

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
