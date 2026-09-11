namespace DiskAtlas.Model;

/// <summary>
/// A directory or file in the reconstructed tree, with sizes already rolled up from
/// everything beneath it.
/// </summary>
public sealed class DiskNode
{
    private List<DiskNode>? _children;

    internal DiskNode(string name, bool isDirectory)
    {
        Name = name;
        IsDirectory = isDirectory;
    }

    public string Name { get; }

    public bool IsDirectory { get; }

    public bool IsCompressed { get; internal set; }

    public DiskNode? Parent { get; internal set; }

    /// <summary>Space this subtree occupies on the volume.</summary>
    public long SizeOnDisk { get; internal set; }

    /// <summary>Sum of the logical file sizes in this subtree.</summary>
    public long LogicalSize { get; internal set; }

    /// <summary>Number of files in this subtree, directories excluded.</summary>
    public long FileCount { get; internal set; }

    public IReadOnlyList<DiskNode> Children => (IReadOnlyList<DiskNode>?)_children ?? [];

    public bool HasChildren => _children is { Count: > 0 };

    internal void AddChild(DiskNode child)
    {
        _children ??= [];
        _children.Add(child);
        child.Parent = this;
    }

    internal void SortChildrenBySize()
    {
        _children?.Sort(static (left, right) => right.SizeOnDisk.CompareTo(left.SizeOnDisk));
    }

    public string FullPath
    {
        get
        {
            Stack<string> parts = new();
            for (DiskNode? node = this; node is not null; node = node.Parent)
            {
                parts.Push(node.Name);
            }

            return string.Join('\\', parts).Replace(@"\\", @"\");
        }
    }

    public override string ToString() => $"{Name} ({FormatSize(SizeOnDisk)})";

    public static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB", "PB"];
        double value = bytes;
        int unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{bytes:N0} {units[unit]}"
            : $"{value:N1} {units[unit]}";
    }
}
