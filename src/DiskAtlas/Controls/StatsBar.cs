using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace DiskAtlas.Controls;

/// <summary>
/// The row of headline numbers under the toolbar. Each figure gets a large value and a
/// small caption, so the scan result reads at a glance instead of as a sentence.
/// </summary>
[DesignerCategory("Code")]
internal sealed class StatsBar : Control
{
    private readonly List<(string Caption, string Value, Color Accent)> _items = [];

    public StatsBar()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw,
            true);

        BackColor = Theme.Background;
    }

    public void Clear()
    {
        _items.Clear();
        Invalidate();
    }

    public void Set(params (string Caption, string Value, Color Accent)[] items)
    {
        _items.Clear();
        _items.AddRange(items);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.Clear(BackColor);

        if (_items.Count == 0)
        {
            return;
        }

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int available = ClientSize.Width - Padding.Left - Padding.Right;
        int gap = 12;
        int cardWidth = (available - (gap * (_items.Count - 1))) / Math.Max(1, _items.Count);
        int x = Padding.Left;

        using var valueBrush = new SolidBrush(Theme.TextPrimary);
        using var captionBrush = new SolidBrush(Theme.TextMuted);

        foreach ((string caption, string value, Color accent) in _items)
        {
            var card = new Rectangle(x, Padding.Top, cardWidth, ClientSize.Height - Padding.Top - Padding.Bottom);
            Theme.FillRoundedRectangle(graphics, card, 6, Theme.Surface);

            // A short accent bar on the left edge ties the figure to its colour in the map.
            using (var accentBrush = new SolidBrush(accent))
            using (GraphicsPath accentPath = Theme.RoundedPath(
                new Rectangle(card.X, card.Y + 10, 3, card.Height - 20), 2))
            {
                graphics.FillPath(accentBrush, accentPath);
            }

            // An empty figure is drawn muted so it reads as "not scanned yet".
            bool placeholder = value is "—" or "";
            using (var brush = placeholder ? new SolidBrush(Theme.TextMuted) : null)
            {
                graphics.DrawString(value, Theme.HeadlineFont, brush ?? valueBrush, card.X + 16, card.Y + 6);
            }
            graphics.DrawString(caption, Theme.CaptionFont, captionBrush, card.X + 18, card.Bottom - 22);

            x += cardWidth + gap;
        }
    }
}

/// <summary>A thin accent coloured progress line, because the themed bar cannot be recoloured.</summary>
[DesignerCategory("Code")]
internal sealed class ProgressStripe : Control
{
    private double _value;

    public ProgressStripe()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw,
            true);

        BackColor = Theme.Border;
        Height = 3;
    }

    /// <summary>Progress between zero and one.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Value
    {
        get => _value;
        set
        {
            double clamped = Math.Clamp(value, 0, 1);
            if (Math.Abs(clamped - _value) < 0.002)
            {
                return;
            }

            _value = clamped;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);

        int width = (int)(ClientSize.Width * _value);
        if (width <= 0)
        {
            return;
        }

        using var brush = new LinearGradientBrush(
            new Rectangle(0, 0, Math.Max(1, width), Math.Max(1, ClientSize.Height)),
            Theme.Accent,
            Color.FromArgb(0x38, 0xBD, 0xF8),
            LinearGradientMode.Horizontal);

        e.Graphics.FillRectangle(brush, 0, 0, width, ClientSize.Height);
    }
}
