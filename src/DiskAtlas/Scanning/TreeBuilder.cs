using DiskAtlas.Model;
using DiskAtlas.Ntfs;

namespace DiskAtlas.Scanning;

/// <summary>
/// Turns the flat table of records into a directory tree. Records only carry a parent
/// reference, so the hierarchy has to be rebuilt from the bottom up.
/// </summary>
internal static class TreeBuilder
{
    private const string OrphanFolderName = "[disconnected]";

    public static DiskNode Build(RawScan scan, string volumeLabel, CancellationToken cancellationToken)
    {
        DiskNode?[] nodes = new DiskNode?[scan.Capacity];

        // Directories first, so every file has somewhere to attach.
        for (long record = 0; record < scan.Capacity; record++)
        {
            ref RawEntry entry = ref scan[record];
            if (!entry.Present || !entry.IsDirectory)
            {
                continue;
            }

            // NTFS stores the root directory under the name ".", which is not what anyone
            // wants to read at the top of the tree or at the front of a path.
            nodes[record] = record == MftScanner.RootRecordNumber
                ? new DiskNode(volumeLabel, isDirectory: true)
                : new DiskNode(entry.Name ?? "?", isDirectory: true);
        }

        cancellationToken.ThrowIfCancellationRequested();

        DiskNode root = nodes[MftScanner.RootRecordNumber] ?? new DiskNode(volumeLabel, isDirectory: true);
        nodes[MftScanner.RootRecordNumber] = root;

        DiskNode? orphans = null;

        // Link directories to their parents. The root points at itself, which would
        // otherwise create a cycle.
        for (long record = 0; record < nodes.Length; record++)
        {
            DiskNode? node = nodes[record];
            if (node is null || record == MftScanner.RootRecordNumber)
            {
                continue;
            }

            long parent = scan[record].ParentRecordNumber;
            DiskNode? parentNode = parent >= 0 && parent < nodes.Length ? nodes[parent] : null;

            if (parentNode is null || ReferenceEquals(parentNode, node))
            {
                orphans ??= CreateOrphanFolder(root);
                parentNode = orphans;
            }

            parentNode.AddChild(node);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Files contribute their size to the directory that holds them. A file with
        // several hard links still appears once in the table, so its data is counted once.
        for (long record = 0; record < scan.Capacity; record++)
        {
            ref RawEntry entry = ref scan[record];
            if (!entry.Present || entry.IsDirectory)
            {
                continue;
            }

            long parent = entry.ParentRecordNumber;
            DiskNode? parentNode = parent >= 0 && parent < nodes.Length ? nodes[parent] : null;

            if (parentNode is null)
            {
                orphans ??= CreateOrphanFolder(root);
                parentNode = orphans;
            }

            var file = new DiskNode(entry.Name ?? "?", isDirectory: false)
            {
                SizeOnDisk = entry.SizeOnDisk,
                LogicalSize = entry.LogicalSize,
                FileCount = 1,
                IsCompressed = entry.IsCompressed,
            };

            parentNode.AddChild(file);
        }

        cancellationToken.ThrowIfCancellationRequested();

        Aggregate(root);
        return root;
    }

    private static DiskNode CreateOrphanFolder(DiskNode root)
    {
        var orphans = new DiskNode(OrphanFolderName, isDirectory: true);
        root.AddChild(orphans);
        return orphans;
    }

    /// <summary>
    /// Rolls sizes up the tree. This runs iteratively because a deep directory structure
    /// would overflow the stack on a recursive walk.
    /// </summary>
    private static void Aggregate(DiskNode root)
    {
        Stack<(DiskNode Node, bool ChildrenDone)> pending = new();
        pending.Push((root, false));

        while (pending.Count > 0)
        {
            (DiskNode node, bool childrenDone) = pending.Pop();

            if (!node.IsDirectory)
            {
                continue;
            }

            if (!childrenDone)
            {
                pending.Push((node, true));
                foreach (DiskNode child in node.Children)
                {
                    pending.Push((child, false));
                }

                continue;
            }

            long sizeOnDisk = 0;
            long logicalSize = 0;
            long fileCount = 0;

            foreach (DiskNode child in node.Children)
            {
                sizeOnDisk += child.SizeOnDisk;
                logicalSize += child.LogicalSize;
                fileCount += child.FileCount;
            }

            node.SizeOnDisk = sizeOnDisk;
            node.LogicalSize = logicalSize;
            node.FileCount = fileCount;
            node.SortChildrenBySize();
        }
    }
}
