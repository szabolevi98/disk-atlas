using System.ComponentModel;
using System.Drawing.Drawing2D;
using DiskAtlas.Rendering;

namespace DiskAtlas.Controls;

/// <summary>Shows which colour means which kind of file in the treemap.</summary>
[DesignerCategory("Code")]
internal sealed class LegendControl : Control
{
    private const int SwatchSize = 10;
    private const int SwatchGap = 7;
    private const int EntryGap = 18;

    public LegendControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw,
            true);

        BackColor = Theme.Surface;
        ForeColor = Theme.TextSecondary;
        Font = Theme.CaptionFont;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.Clear(BackColor);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var textBrush = new SolidBrush(ForeColor);
        float x = Padding.Left;
        float middle = ClientSize.Height / 2f;

        foreach (FileTypeColors.Category category in FileTypeColors.All)
        {
            SizeF textSize = graphics.MeasureString(category.Name, Font);
            float entryWidth = SwatchSize + SwatchGap + textSize.Width;

            if (x + entryWidth > ClientSize.Width - Padding.Right)
            {
                break;
            }

            using (var swatch = new SolidBrush(category.Color))
            using (GraphicsPath path = Theme.RoundedPath(
                new Rectangle((int)x, (int)(middle - (SwatchSize / 2f)), SwatchSize, SwatchSize), 2))
            {
                graphics.FillPath(swatch, path);
            }

            graphics.DrawString(
                category.Name,
                Font,
                textBrush,
                x + SwatchSize + SwatchGap,
                middle - (textSize.Height / 2f));

            x += entryWidth + EntryGap;
        }
    }
}
