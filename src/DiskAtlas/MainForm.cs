using DiskAtlas.Model;
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

    private CancellationTokenSource? _scanCancellation;
    private ScanResult? _result;

    public MainForm()
    {
        InitializeComponent();
        LoadWindowIcon();
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

    private void MainForm_Load(object? sender, EventArgs e)
    {
        LoadVolumes();
        UpdateStatus(Elevation.IsElevated
            ? "Select a volume and start a scan."
            : "Select a volume and start a scan. Reading the Master File Table will ask for administrator rights.");
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

        if (!Elevation.IsElevated && !RequestElevation())
        {
            return;
        }

        await RunScanAsync(volume);
    }

    /// <summary>
    /// Asks the user whether the application may restart with administrator rights, and
    /// closes the current instance once the elevated one has started.
    /// </summary>
    private bool RequestElevation()
    {
        DialogResult answer = MessageBox.Show(
            this,
            "Reading the Master File Table needs administrator rights.\r\n\r\nRestart Disk Atlas as administrator?",
            "Administrator rights required",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer != DialogResult.Yes)
        {
            return false;
        }

        if (Elevation.TryRestartElevated())
        {
            Close();
            return false;
        }

        UpdateStatus("The scan was cancelled because the application was not elevated.");
        return false;
    }

    private async Task RunScanAsync(VolumeInfo volume)
    {
        _scanCancellation = new CancellationTokenSource();
        SetScanning(true);

        var progress = new Progress<double>(fraction =>
            scanProgressBar.Value = (int)Math.Clamp(fraction * 100, 0, 100));

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
            MessageBox.Show(
                this,
                exception.Message,
                "Scan failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetScanning(false);
            _scanCancellation.Dispose();
            _scanCancellation = null;
        }
    }

    private void CancelButton_Click(object? sender, EventArgs e) => _scanCancellation?.Cancel();

    private void RefreshVolumesButton_Click(object? sender, EventArgs e) => LoadVolumes();

    private void SetScanning(bool scanning)
    {
        scanButton.Enabled = !scanning;
        cancelButton.Enabled = scanning;
        refreshVolumesButton.Enabled = !scanning;
        volumeComboBox.Enabled = !scanning;
        scanProgressBar.Visible = scanning;
        scanProgressBar.Value = 0;

        if (scanning)
        {
            folderTreeView.Nodes.Clear();
            contentListView.Items.Clear();
        }
    }

    private void ShowResult(ScanResult result)
    {
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

        UpdateStatus(
            $"{result.Volume.DriveLetter}: — {result.FileCount:N0} files in {result.DirectoryCount:N0} folders, " +
            $"{DiskNode.FormatSize(result.Root.SizeOnDisk)} on disk, read in {result.Duration.TotalSeconds:N2} s");
    }

    private static TreeNode CreateNode(DiskNode node)
    {
        var treeNode = new TreeNode($"{node.Name}  —  {DiskNode.FormatSize(node.SizeOnDisk)}")
        {
            Tag = node,
        };

        if (node.Children.Any(child => child.IsDirectory))
        {
            treeNode.Nodes.Add(PlaceholderKey, PlaceholderKey);
        }

        return treeNode;
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
            foreach (DiskNode child in node.Children)
            {
                if (child.IsDirectory)
                {
                    treeNode.Nodes.Add(CreateNode(child));
                }
            }
        }
        finally
        {
            folderTreeView.EndUpdate();
        }
    }

    private void FolderTreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is DiskNode node)
        {
            ShowChildren(node);
        }
    }

    /// <summary>
    /// Lists the direct children of a folder, largest first. The tree is already sorted by
    /// size, so the order comes for free.
    /// </summary>
    private void ShowChildren(DiskNode node)
    {
        contentListView.BeginUpdate();
        try
        {
            contentListView.Items.Clear();

            foreach (DiskNode child in node.Children)
            {
                var item = new ListViewItem(child.Name) { Tag = child };
                item.SubItems.Add(DiskNode.FormatSize(child.SizeOnDisk));
                item.SubItems.Add(DiskNode.FormatSize(child.LogicalSize));
                item.SubItems.Add(child.IsDirectory ? $"{child.FileCount:N0}" : string.Empty);
                item.SubItems.Add(child.IsDirectory ? "Folder" : child.IsCompressed ? "File (compressed)" : "File");
                contentListView.Items.Add(item);
            }
        }
        finally
        {
            contentListView.EndUpdate();
        }
    }

    /// <summary>Double clicking a folder in the list moves the tree selection into it.</summary>
    private void ContentListView_DoubleClick(object? sender, EventArgs e)
    {
        if (contentListView.SelectedItems.Count == 0
            || contentListView.SelectedItems[0].Tag is not DiskNode { IsDirectory: true } target
            || folderTreeView.SelectedNode is not TreeNode parent)
        {
            return;
        }

        parent.Expand();

        foreach (TreeNode candidate in parent.Nodes)
        {
            if (ReferenceEquals(candidate.Tag, target))
            {
                folderTreeView.SelectedNode = candidate;
                return;
            }
        }
    }

    private void UpdateStatus(string text) => statusLabel.Text = text;
}
