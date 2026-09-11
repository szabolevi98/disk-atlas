using System.Drawing.Drawing2D;
using DiskAtlas.Model;
using DiskAtlas.Rendering;
using DiskAtlas.Scanning;

namespace DiskAtlas;

public partial class MainForm : Form
{
    /// <summary>
    /// Placeholder child that gives a collapsed folder its expand arrow without building
    /// the whole subtree up front. A volume can hold millions of nodes, so the tree is
    /// filled one level at a time.
    /// </summary>
    private const string PlaceholderKey = "__placeholder__";

    /// <summary>Marks the row standing in for the folders past the display limit.</summary>
    private const string OverflowKey = "__overflow__";

    /// <summary>
    /// How many subfolders one tree level shows. An owner drawn TreeView spends about
    /// seven tenths of a millisecond per inserted node, so a folder like WinSxS with
    /// twenty thousand subfolders would lock the window up for twenty seconds. The tree
    /// shows the largest folders, which is what the question is about, and the list below
    /// still holds every child.
    /// </summary>
    private const int TreeChildLimit = 250;

    private CancellationTokenSource? _scanCancellation;
    private ScanResult? _result;

    /// <summary>
    /// Rows behind the virtual list. A folder can hold tens of thousands of files, and
    /// building a ListViewItem for each of them froze the window, so the control asks for
    /// rows as it draws them instead.
    /// </summary>
    private IReadOnlyList<DiskNode> _rows = [];

    /// <summary>Size of the folder the rows belong to, used for the share column.</summary>
    private long _rowsTotal = 1;

    public MainForm()
    {
        InitializeComponent();
        LoadWindowIcon();
        ApplyFonts();
    }

    /// <summary>
    /// Takes the window icon from the embedded multi resolution file rather than the
    /// designer resource, so the title bar and the task bar both get a crisp size.
    /// </summary>
    private void LoadWindowIcon()
    {
        using Stream? stream = typeof(MainForm).Assembly.GetManifestResourceStream("DiskAtlas.app.ico");
        if (stream is not null)
        {
            Icon = new Icon(stream);
        }
    }

    private void ApplyFonts()
    {
        Font = Theme.UiFont;
        titleLabel.Font = new Font("Segoe UI Light", 17F);
        subtitleLabel.Font = Theme.CaptionFont;
        scanButton.Font = Theme.UiFontBold;
        cancelScanButton.Font = Theme.UiFont;
        aboutButton.Font = Theme.UiFont;
        statusLabel.Font = Theme.CaptionFont;
        hoverLabel.Font = Theme.CaptionFont;

        // Flat buttons keep a one pixel system border unless it is painted away.
        scanButton.FlatAppearance.BorderSize = 0;
        cancelScanButton.FlatAppearance.BorderColor = Theme.Border;
        cancelScanButton.FlatAppearance.MouseOverBackColor = Theme.SurfaceHover;
        aboutButton.FlatAppearance.BorderColor = Theme.Border;
        aboutButton.FlatAppearance.MouseOverBackColor = Theme.SurfaceHover;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Native.NativeMethods.UseDarkTitleBar(Handle);

        // The scroll bars belong to the native control, not to the managed wrapper, so
        // they only turn dark when the control itself is switched to the dark visual style.
        Native.NativeMethods.UseDarkStyle(folderTreeView.Handle);
        Native.NativeMethods.UseDarkStyle(contentListView.Handle);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        // Windows applies the attribute to the visible frame, so it is set again once the
        // window actually exists on screen.
        Native.NativeMethods.UseDarkTitleBar(Handle);
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        FitToScreen();
        LoadVolumes();
        ShowIdleStats();
        StretchLastColumn();

        UpdateStatus(Elevation.IsElevated
            ? "Pick a volume and read its Master File Table."
            : "Not running as administrator, so the volume cannot be opened for raw reading.");
    }

    /// <summary>
    /// Keeps the window inside the screen it opens on. The designer size is meant for a
    /// desktop monitor and is taller than a 1366 by 768 laptop can show.
    /// </summary>
    private void FitToScreen()
    {
        Rectangle workingArea = Screen.FromControl(this).WorkingArea;
        Size fitted = WindowSizing.FitWithin(Size, MinimumSize, workingArea);

        if (fitted == Size)
        {
            return;
        }

        Size = fitted;

        // The start position was worked out for the larger size, so it has to be centred
        // again for the one that actually fits.
        Location = WindowSizing.CenterWithin(fitted, workingArea);
    }

    private void LoadVolumes()
    {
        object? previous = volumeComboBox.SelectedItem;

        volumeComboBox.Items.Clear();
        foreach (VolumeInfo volume in VolumeInfo.EnumerateReadyVolumes())
        {
            volumeComboBox.Items.Add(volume);
        }

        if (volumeComboBox.Items.Count == 0)
        {
            scanButton.Enabled = false;
            UpdateStatus("No readable volume was found.");
            return;
        }

        scanButton.Enabled = true;

        int index = previous is null ? -1 : volumeComboBox.Items.IndexOf(previous);
        volumeComboBox.SelectedIndex = index >= 0 ? index : 0;
    }

    private void ShowIdleStats() => statsBar.Set(
        ("Files", "—", Theme.Accent),
        ("Folders", "—", FileTypeColors.Programs.Color),
        ("Size on disk", "—", FileTypeColors.Video.Color),
        ("Scan time", "—", FileTypeColors.Images.Color));

    /// <summary>The drop down is owner drawn because a ComboBox ignores its BackColor otherwise.</summary>
    private void VolumeComboBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
        {
            return;
        }

        bool highlighted = (e.State & DrawItemState.Selected) != 0;
        using var background = new SolidBrush(highlighted ? Theme.SurfaceHover : Theme.SurfaceRaised);
        e.Graphics.FillRectangle(background, e.Bounds);

        using var text = new SolidBrush(Theme.TextPrimary);
        var layout = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height);
        using var format = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        e.Graphics.DrawString(volumeComboBox.Items[e.Index]?.ToString(), Theme.UiFont, text, layout, format);
    }

    private async void ScanButton_Click(object? sender, EventArgs e)
    {
        if (volumeComboBox.SelectedItem is not VolumeInfo volume)
        {
            return;
        }

        if (!volume.SupportsFastScan)
        {
            MessageBox.Show(
                this,
                $"{volume.DriveLetter}: is formatted as {volume.FileSystem}. Only NTFS volumes can be read through the Master File Table.",
                "Unsupported file system",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // The manifest asks for elevation at startup, so this only trips when the assembly
        // is launched in a way that bypasses it, such as running the dll directly.
        if (!Elevation.IsElevated)
        {
            if (MessageBox.Show(
                    this,
                    "Reading the Master File Table needs administrator rights.\r\n\r\nRestart Disk Atlas as administrator?",
                    "Administrator rights required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes
                && Elevation.TryRestartElevated())
            {
                Close();
            }

            return;
        }

        await RunScanAsync(volume);
    }

    private async Task RunScanAsync(VolumeInfo volume)
    {
        _scanCancellation = new CancellationTokenSource();
        SetScanning(true);

        var progress = new Progress<double>(fraction => progressStripe.Value = fraction);

        try
        {
            UpdateStatus($"Reading the Master File Table of {volume.DriveLetter}: ...");
            _result = await DiskScanner.ScanAsync(volume, progress, _scanCancellation.Token);
            ShowResult(_result);
        }
        catch (OperationCanceledException)
        {
            UpdateStatus("Scan cancelled.");
        }
        catch (Exception exception)
        {
            UpdateStatus("Scan failed.");
            MessageBox.Show(this, exception.Message, "Scan failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetScanning(false);
            _scanCancellation.Dispose();
            _scanCancellation = null;
        }
    }

    private void CancelScanButton_Click(object? sender, EventArgs e) => _scanCancellation?.Cancel();

    private void AboutButton_Click(object? sender, EventArgs e)
    {
        using var about = new AboutForm();
        about.ShowDialog(this);
    }

    private void SetScanning(bool scanning)
    {
        scanButton.Enabled = !scanning;
        cancelScanButton.Enabled = scanning;
        volumeComboBox.Enabled = !scanning;
        progressStripe.Value = 0;
        Cursor = scanning ? Cursors.AppStarting : Cursors.Default;

        if (scanning)
        {
            folderTreeView.Nodes.Clear();
            _rows = [];
            contentListView.VirtualListSize = 0;
            treemapControl.Root = null;
            ShowIdleStats();
        }
    }

    private void ShowResult(ScanResult result)
    {
        statsBar.Set(
            ("Files", $"{result.FileCount:N0}", Theme.Accent),
            ("Folders", $"{result.DirectoryCount:N0}", FileTypeColors.Programs.Color),
            ("Size on disk", DiskNode.FormatSize(result.Root.SizeOnDisk), FileTypeColors.Video.Color),
            ("Scan time", $"{result.Duration.TotalSeconds:N2} s", FileTypeColors.Images.Color));

        folderTreeView.BeginUpdate();
        try
        {
            folderTreeView.Nodes.Clear();
            TreeNode rootNode = CreateNode(result.Root);
            folderTreeView.Nodes.Add(rootNode);
            rootNode.Expand();
            folderTreeView.SelectedNode = rootNode;
        }
        finally
        {
            folderTreeView.EndUpdate();
        }

        treemapControl.Root = result.Root;

        double perSecond = result.Duration.TotalSeconds > 0
            ? (result.FileCount + result.DirectoryCount) / result.Duration.TotalSeconds
            : 0;

        UpdateStatus($"{result.Volume.DriveLetter}: mapped at {perSecond:N0} records per second");
    }

    private static TreeNode CreateNode(DiskNode node)
    {
        var treeNode = new TreeNode(node.Name) { Tag = node };

        if (node.Children.Any(child => child.IsDirectory))
        {
            treeNode.Nodes.Add(PlaceholderKey, PlaceholderKey);
        }

        return treeNode;
    }

    // ---------------------------------------------------------------- folder tree

    /// <summary>
    /// The tree is drawn by hand: the stock control cannot be told to use a dark selection
    /// colour, and a size bar behind each row makes the big folders findable at a glance.
    /// </summary>
    private void FolderTreeView_DrawNode(object? sender, DrawTreeNodeEventArgs e)
    {
        if (e.Node is null)
        {
            return;
        }

        Graphics graphics = e.Graphics;
        var row = new Rectangle(0, e.Bounds.Y, folderTreeView.ClientSize.Width, e.Bounds.Height);
        bool selected = (e.State & TreeNodeStates.Selected) != 0;

        using (var background = new SolidBrush(selected ? Theme.Selection : folderTreeView.BackColor))
        {
            graphics.FillRectangle(background, row);
        }

        if (e.Node.Tag is not DiskNode node)
        {
            // The summary row has no folder behind it, so it only gets its label.
            using var summary = new SolidBrush(Theme.TextMuted);
            using var summaryFormat = new StringFormat { LineAlignment = StringAlignment.Center };
            graphics.DrawString(
                e.Node.Text,
                Theme.CaptionFont,
                summary,
                new RectangleF((e.Node.Level * folderTreeView.Indent) + 26, row.Y, row.Width - 40, row.Height),
                summaryFormat);
            return;
        }

        // A faint bar showing how much of the parent this folder takes.
        double share = node.Parent is { SizeOnDisk: > 0 } parent
            ? (double)node.SizeOnDisk / parent.SizeOnDisk
            : 1.0;

        int barWidth = (int)(row.Width * Math.Clamp(share, 0, 1));
        if (barWidth > 0)
        {
            using var bar = new SolidBrush(Color.FromArgb(selected ? 46 : 26, Theme.Accent));
            graphics.FillRectangle(bar, row.X, row.Y, barWidth, row.Height);
        }

        if (selected)
        {
            using var marker = new SolidBrush(Theme.Accent);
            graphics.FillRectangle(marker, row.X, row.Y, 2, row.Height);
        }

        int indent = (e.Node.Level * folderTreeView.Indent) + 10;

        if (e.Node.Nodes.Count > 0)
        {
            DrawExpander(graphics, new Rectangle(indent, row.Y + (row.Height / 2) - 4, 8, 8), e.Node.IsExpanded);
        }

        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        string size = DiskNode.FormatSize(node.SizeOnDisk);
        SizeF sizeExtent = graphics.MeasureString(size, Theme.UiFont);

        using (var sizeBrush = new SolidBrush(selected ? Theme.TextPrimary : Theme.TextSecondary))
        using (var format = new StringFormat { LineAlignment = StringAlignment.Center })
        {
            graphics.DrawString(
                size, Theme.UiFont, sizeBrush,
                new RectangleF(row.Right - sizeExtent.Width - 12, row.Y, sizeExtent.Width, row.Height),
                format);
        }

        int textLeft = indent + 16;
        var nameBounds = new RectangleF(
            textLeft, row.Y, Math.Max(10, row.Right - sizeExtent.Width - 22 - textLeft), row.Height);

        using (var nameBrush = new SolidBrush(Theme.TextPrimary))
        using (var format = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
        })
        {
            graphics.DrawString(node.Name, selected ? Theme.UiFontBold : Theme.UiFont, nameBrush, nameBounds, format);
        }
    }

    private static void DrawExpander(Graphics graphics, Rectangle bounds, bool expanded)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        PointF[] triangle = expanded
            ?
            [
                new PointF(bounds.Left, bounds.Top + 1),
                new PointF(bounds.Right, bounds.Top + 1),
                new PointF(bounds.Left + (bounds.Width / 2f), bounds.Bottom),
            ]
            :
            [
                new PointF(bounds.Left + 1, bounds.Top),
                new PointF(bounds.Right - 1, bounds.Top + (bounds.Height / 2f)),
                new PointF(bounds.Left + 1, bounds.Bottom),
            ];

        using var brush = new SolidBrush(Theme.TextMuted);
        graphics.FillPolygon(brush, triangle);
        graphics.SmoothingMode = SmoothingMode.None;
    }

    /// <summary>The built in expander is hidden, so clicks on the drawn triangle are handled here.</summary>
    private void FolderTreeView_MouseDown(object? sender, MouseEventArgs e)
    {
        TreeNode? node = folderTreeView.GetNodeAt(e.X, e.Y);
        if (node is null || node.Nodes.Count == 0)
        {
            return;
        }

        int indent = (node.Level * folderTreeView.Indent) + 10;
        if (e.X >= indent - 4 && e.X <= indent + 14)
        {
            node.Toggle();
        }
    }

    private void FolderTreeView_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        if (e.Node is not { Tag: DiskNode node } treeNode)
        {
            return;
        }

        if (treeNode.Nodes.Count != 1 || treeNode.Nodes[0].Name != PlaceholderKey)
        {
            return;
        }

        folderTreeView.BeginUpdate();
        try
        {
            treeNode.Nodes.Clear();
            treeNode.Nodes.AddRange(BuildChildNodes(node));
        }
        finally
        {
            folderTreeView.EndUpdate();
        }
    }

    /// <summary>
    /// Builds the rows for one level: the largest subfolders, and a summary of whatever
    /// did not fit. Children are already sorted largest first.
    /// </summary>
    private static TreeNode[] BuildChildNodes(DiskNode node)
    {
        List<DiskNode> directories = [];
        foreach (DiskNode child in node.Children)
        {
            if (child.IsDirectory)
            {
                directories.Add(child);
            }
        }

        int shown = Math.Min(directories.Count, TreeChildLimit);
        int hidden = directories.Count - shown;

        var nodes = new TreeNode[shown + (hidden > 0 ? 1 : 0)];
        for (int i = 0; i < shown; i++)
        {
            nodes[i] = CreateNode(directories[i]);
        }

        if (hidden > 0)
        {
            long hiddenSize = 0;
            for (int i = shown; i < directories.Count; i++)
            {
                hiddenSize += directories[i].SizeOnDisk;
            }

            nodes[shown] = new TreeNode($"{hidden:N0} smaller folders — {DiskNode.FormatSize(hiddenSize)}")
            {
                Name = OverflowKey,
            };
        }

        return nodes;
    }

    private void FolderTreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is not DiskNode node)
        {
            return;
        }

        ShowChildren(node);
        treemapControl.SelectedNode = node;
    }

    // ------------------------------------------------------------------- treemap

    private void TreemapControl_NodeActivated(object? sender, DiskNode node)
    {
        // Select the containing folder, since a single file has no row in the tree.
        DiskNode? folder = node.IsDirectory ? node : node.Parent;
        if (folder is null)
        {
            return;
        }

        TreeNode? target = FindOrExpand(folder);
        if (target is not null)
        {
            folderTreeView.SelectedNode = target;
            target.EnsureVisible();
        }

        if (!node.IsDirectory)
        {
            SelectInList(node);
        }
    }

    /// <summary>
    /// Walks from the root down to the wanted folder, expanding along the way so the lazily
    /// filled tree actually contains the node before it is selected.
    /// </summary>
    private TreeNode? FindOrExpand(DiskNode target)
    {
        Stack<DiskNode> path = new();
        for (DiskNode? node = target; node is not null; node = node.Parent)
        {
            path.Push(node);
        }

        if (path.Count == 0 || folderTreeView.Nodes.Count == 0)
        {
            return null;
        }

        path.Pop();
        TreeNode current = folderTreeView.Nodes[0];

        while (path.Count > 0)
        {
            DiskNode wanted = path.Pop();
            current.Expand();

            TreeNode? next = null;
            foreach (TreeNode candidate in current.Nodes)
            {
                if (ReferenceEquals(candidate.Tag, wanted))
                {
                    next = candidate;
                    break;
                }
            }

            if (next is null)
            {
                // The folder sits past the display limit. Adding just this one keeps the
                // list able to navigate anywhere without filling the tree.
                next = CreateNode(wanted);

                int overflowRow = current.Nodes.IndexOfKey(OverflowKey);
                if (overflowRow >= 0)
                {
                    current.Nodes.Insert(overflowRow, next);
                }
                else
                {
                    current.Nodes.Add(next);
                }
            }

            current = next;
        }

        return current;
    }

    private void SelectInList(DiskNode node)
    {
        for (int index = 0; index < _rows.Count; index++)
        {
            if (!ReferenceEquals(_rows[index], node))
            {
                continue;
            }

            contentListView.SelectedIndices.Clear();
            contentListView.SelectedIndices.Add(index);
            contentListView.EnsureVisible(index);
            return;
        }
    }

    private void TreemapControl_NodeHovered(object? sender, DiskNode? node)
    {
        hoverLabel.Text = node is null
            ? string.Empty
            : $"{node.FullPath}   ·   {DiskNode.FormatSize(node.SizeOnDisk)}";
    }

    // ---------------------------------------------------------------- file list

    /// <summary>Lists the direct children of a folder, largest first.</summary>
    private void ShowChildren(DiskNode node)
    {
        _rows = node.Children;
        _rowsTotal = Math.Max(1, node.SizeOnDisk);

        // Dropping to zero first clears any selection that would point past the new end.
        contentListView.VirtualListSize = 0;
        contentListView.VirtualListSize = _rows.Count;
        contentListView.Invalidate();

        if (_rows.Count > 0)
        {
            contentListView.EnsureVisible(0);
        }

        // A scroll bar may have appeared or gone, changing how much room there is.
        StretchLastColumn();
    }

    /// <summary>Builds a row on demand, which is why a huge folder opens instantly.</summary>
    private void ContentListView_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
    {
        if (e.ItemIndex < 0 || e.ItemIndex >= _rows.Count)
        {
            e.Item = new ListViewItem();
            return;
        }

        DiskNode child = _rows[e.ItemIndex];

        var item = new ListViewItem(child.Name) { Tag = child };
        item.SubItems.Add(DiskNode.FormatSize(child.SizeOnDisk));
        item.SubItems.Add(((double)child.SizeOnDisk / _rowsTotal).ToString("P1"));
        item.SubItems.Add(DiskNode.FormatSize(child.LogicalSize));
        item.SubItems.Add(child.IsDirectory ? $"{child.FileCount:N0}" : string.Empty);
        item.SubItems.Add(child.IsDirectory
            ? "Folder"
            : child.IsCompressed
                ? $"{FileTypeColors.Categorize(child.Name).Name}, compressed"
                : FileTypeColors.Categorize(child.Name).Name);

        e.Item = item;
    }

    private void ContentListView_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        // Anything past the last column is painted white by Windows, and the drawing here
        // is clipped to the column, so it cannot be covered from this handler. The last
        // column is stretched to fill the control instead, leaving no strip to paint.
        using (var background = new SolidBrush(Theme.Background))
        {
            e.Graphics.FillRectangle(background, e.Bounds);
        }

        using (var separator = new Pen(Theme.Border))
        {
            e.Graphics.DrawLine(separator, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        using var text = new SolidBrush(Theme.TextMuted);
        using var format = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Alignment = e.Header?.TextAlign == HorizontalAlignment.Right ? StringAlignment.Far : StringAlignment.Near,
            Trimming = StringTrimming.EllipsisCharacter,
        };

        var layout = Rectangle.Inflate(e.Bounds, -8, 0);
        e.Graphics.DrawString(e.Header?.Text, Theme.CaptionFont, text, layout, format);
    }

    private void ContentListView_DrawItem(object? sender, DrawListViewItemEventArgs e) => e.DrawDefault = false;

    private void ContentListView_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.Item is null)
        {
            return;
        }

        bool selected = e.Item.Selected;
        Color background = selected
            ? Theme.Selection
            : e.ItemIndex % 2 == 0 ? contentListView.BackColor : Theme.SurfaceRaised;

        using (var brush = new SolidBrush(background))
        {
            e.Graphics.FillRectangle(brush, e.Bounds);
        }

        var node = e.Item.Tag as DiskNode;

        // The share column is drawn as a bar rather than a number.
        if (e.ColumnIndex == 2 && node is not null)
        {
            DrawShareBar(e, node);
            return;
        }

        if (e.ColumnIndex == 0)
        {
            DrawTypeSwatch(e, node);
        }

        bool rightAligned = contentListView.Columns[e.ColumnIndex].TextAlign == HorizontalAlignment.Right;
        int leftPadding = e.ColumnIndex == 0 ? 24 : 8;

        using var text = new SolidBrush(selected ? Theme.TextPrimary : Theme.TextSecondary);
        using var format = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Alignment = rightAligned ? StringAlignment.Far : StringAlignment.Near,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
        };

        var layout = new Rectangle(
            e.Bounds.X + leftPadding,
            e.Bounds.Y,
            Math.Max(4, e.Bounds.Width - leftPadding - 8),
            e.Bounds.Height);

        Font font = e.ColumnIndex == 0 ? Theme.UiFont : Theme.CaptionFont;
        e.Graphics.DrawString(e.SubItem?.Text, font, text, layout, format);
    }

    private static void DrawShareBar(DrawListViewSubItemEventArgs e, DiskNode node)
    {
        double share = node.Parent is { SizeOnDisk: > 0 } parent
            ? (double)node.SizeOnDisk / parent.SizeOnDisk
            : 1;

        var track = new Rectangle(e.Bounds.X + 8, e.Bounds.Y + (e.Bounds.Height / 2) - 3, e.Bounds.Width - 50, 6);
        Theme.FillRoundedRectangle(e.Graphics, track, 3, Theme.Border);

        int filled = (int)(track.Width * Math.Clamp(share, 0, 1));
        if (filled > 2)
        {
            Color color = node.IsDirectory ? Theme.Accent : FileTypeColors.Categorize(node.Name).Color;
            Theme.FillRoundedRectangle(e.Graphics, track with { Width = filled }, 3, color);
        }

        using var text = new SolidBrush(Theme.TextMuted);
        using var format = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Far };
        e.Graphics.DrawString(
            share.ToString("P0"),
            Theme.CaptionFont,
            text,
            new RectangleF(track.Right + 2, e.Bounds.Y, 36, e.Bounds.Height),
            format);
    }

    private static void DrawTypeSwatch(DrawListViewSubItemEventArgs e, DiskNode? node)
    {
        Color color = node is null
            ? Theme.TextMuted
            : node.IsDirectory ? Theme.TextSecondary : FileTypeColors.Categorize(node.Name).Color;

        var swatch = new Rectangle(e.Bounds.X + 8, e.Bounds.Y + (e.Bounds.Height / 2) - 4, 8, 8);
        Theme.FillRoundedRectangle(e.Graphics, swatch, node?.IsDirectory == true ? 2 : 4, color);
    }

    /// <summary>Double clicking a folder in the list moves the tree selection into it.</summary>
    private void ContentListView_DoubleClick(object? sender, EventArgs e)
    {
        if (contentListView.SelectedIndices.Count == 0)
        {
            return;
        }

        int index = contentListView.SelectedIndices[0];
        if (index < 0 || index >= _rows.Count || _rows[index] is not { IsDirectory: true } target)
        {
            return;
        }

        TreeNode? node = FindOrExpand(target);
        if (node is not null)
        {
            folderTreeView.SelectedNode = node;
            node.EnsureVisible();
        }
    }

    /// <summary>Smallest the stretched column is allowed to get before scrolling starts.</summary>
    private const int LastColumnMinimumWidth = 110;

    private bool _adjustingColumns;

    /// <summary>
    /// Grows the last column so the columns together cover the whole control. Windows
    /// paints the area past the last column white and owner drawing cannot reach it, so
    /// the fix is to leave no such area.
    /// </summary>
    private void StretchLastColumn()
    {
        if (_adjustingColumns || contentListView.Columns.Count == 0)
        {
            return;
        }

        _adjustingColumns = true;
        try
        {
            int used = 0;
            for (int i = 0; i < contentListView.Columns.Count - 1; i++)
            {
                used += contentListView.Columns[i].Width;
            }

            contentListView.Columns[^1].Width =
                Math.Max(LastColumnMinimumWidth, contentListView.ClientSize.Width - used);
        }
        finally
        {
            _adjustingColumns = false;
        }
    }

    private void ContentListView_SizeChanged(object? sender, EventArgs e) => StretchLastColumn();

    private void ContentListView_ColumnWidthChanged(object? sender, ColumnWidthChangedEventArgs e) => StretchLastColumn();

    private void UpdateStatus(string text) => statusLabel.Text = text;
}
