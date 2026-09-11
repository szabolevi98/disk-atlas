using System.ComponentModel;
using System.Drawing.Drawing2D;
using DiskAtlas.Model;
using DiskAtlas.Rendering;

namespace DiskAtlas.Controls;

/// <summary>
/// Draws a cushion treemap of a folder and lets the user point at the rectangles. The map
/// is rendered once into a bitmap and only the hover outline is painted per frame, so
/// moving the mouse over a million files stays smooth.
/// </summary>
[DesignerCategory("Code")]
internal sealed class TreemapControl : Control
{
    private TreemapRender? _render;
    private DiskNode? _root;
    private TreemapItem? _hovered;
    private DiskNode? _selected;
    private Rectangle? _selectionBounds;
    private Size _renderedFor;

    public TreemapControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw,
            true);

        BackColor = Theme.Background;
        Font = Theme.UiFont;
    }

    /// <summary>Raised when a rectangle is clicked, so the rest of the window can follow.</summary>
    public event EventHandler<DiskNode>? NodeActivated;

    /// <summary>Raised as the pointer moves, with null when it leaves every rectangle.</summary>
    public event EventHandler<DiskNode?>? NodeHovered;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DiskNode? Root
    {
        get => _root;
        set
        {
            _root = value;
            _hovered = null;
            _selected = null;
            _selectionBounds = null;
            Rebuild();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DiskNode? SelectedNode
    {
        get => _selected;
        set
        {
            _selected = value;

            // Finding the region costs a pass over every rectangle, so it is done once
            // here rather than on each repaint, which would otherwise make hovering
            // crawl as soon as anything was selected.
            _selectionBounds = MeasureOutline(value);
            Invalidate();
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);

        if (ClientSize != _renderedFor)
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        _render?.Dispose();
        _render = null;
        _renderedFor = ClientSize;

        if (_root is not null && ClientSize.Width > 0 && ClientSize.Height > 0)
        {
            _render = TreemapRenderer.Render(_root, ClientSize.Width, ClientSize.Height);
        }

        // The rectangles moved, so the cached region no longer describes anything.
        _selectionBounds = MeasureOutline(_selected);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;

        if (_render is null)
        {
            PaintPlaceholder(graphics);
            return;
        }

        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.DrawImageUnscaled(_render.Image, 0, 0);

        if (_selectionBounds is { } selection)
        {
            using var pen = new Pen(Theme.Accent, 2);
            graphics.SmoothingMode = SmoothingMode.None;
            graphics.DrawRectangle(pen, selection.X, selection.Y, selection.Width - 2, selection.Height - 2);
        }

        if (_hovered is not null)
        {
            using var pen = new Pen(Color.White, 1);
            graphics.SmoothingMode = SmoothingMode.None;
            Rectangle bounds = _hovered.Bounds;
            graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        }
    }

    /// <summary>
    /// Finds the region a folder covers. A folder is drawn as many rectangles, so the
    /// whole subtree has to be traced rather than a single box.
    /// </summary>
    private Rectangle? MeasureOutline(DiskNode? node)
    {
        if (node is null || _render is null || ReferenceEquals(node, _root))
        {
            return null;
        }

        Rectangle? union = null;
        foreach (TreemapItem item in _render.Items)
        {
            if (!IsWithin(item.Node, node))
            {
                continue;
            }

            union = union is null ? item.Bounds : Rectangle.Union(union.Value, item.Bounds);
        }

        return union;
    }

    private static bool IsWithin(DiskNode candidate, DiskNode ancestor)
    {
        for (DiskNode? node = candidate; node is not null; node = node.Parent)
        {
            if (ReferenceEquals(node, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private void PaintPlaceholder(Graphics graphics)
    {
        graphics.Clear(Theme.Background);

        string message = _root is null
            ? "Scan a volume to see its map"
            : "This folder is empty";

        using var brush = new SolidBrush(Theme.TextMuted);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };

        graphics.DrawString(message, Theme.UiFont, brush, ClientRectangle, format);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        TreemapItem? item = _render?.HitTest(e.X, e.Y);
        if (ReferenceEquals(item, _hovered))
        {
            return;
        }

        _hovered = item;
        NodeHovered?.Invoke(this, item?.Node);
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);

        if (_hovered is not null)
        {
            _hovered = null;
            NodeHovered?.Invoke(this, null);
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        TreemapItem? item = _render?.HitTest(e.X, e.Y);
        if (item is not null)
        {
            NodeActivated?.Invoke(this, item.Node);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _render?.Dispose();
        }

        base.Dispose(disposing);
    }
}
