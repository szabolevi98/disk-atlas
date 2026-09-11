using System.Drawing.Drawing2D;

namespace DiskAtlas;

/// <summary>
/// One place for the palette, so the custom drawn controls and the stock Windows Forms
/// ones end up the same colour. The hues follow the application icon.
/// </summary>
internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(0x0F, 0x16, 0x20);
    public static readonly Color Surface = Color.FromArgb(0x17, 0x1F, 0x2C);
    public static readonly Color SurfaceRaised = Color.FromArgb(0x1E, 0x28, 0x38);
    public static readonly Color SurfaceHover = Color.FromArgb(0x27, 0x33, 0x45);
    public static readonly Color Border = Color.FromArgb(0x2B, 0x37, 0x4A);

    public static readonly Color TextPrimary = Color.FromArgb(0xE6, 0xED, 0xF6);
    public static readonly Color TextSecondary = Color.FromArgb(0x93, 0xA4, 0xBA);
    public static readonly Color TextMuted = Color.FromArgb(0x64, 0x74, 0x8B);

    public static readonly Color Accent = Color.FromArgb(0x2D, 0xD4, 0xBF);
    public static readonly Color AccentPressed = Color.FromArgb(0x14, 0xB8, 0xA6);
    public static readonly Color Selection = Color.FromArgb(0x1E, 0x3A, 0x45);

    public static readonly Font UiFont = new("Segoe UI", 9F);
    public static readonly Font UiFontBold = new("Segoe UI Semibold", 9F);
    public static readonly Font HeadlineFont = new("Segoe UI Light", 20F);
    public static readonly Font CaptionFont = new("Segoe UI", 8F);

    /// <summary>Paints a rounded panel used by the cards in the header.</summary>
    public static void FillRoundedRectangle(Graphics graphics, Rectangle bounds, int radius, Color fill)
    {
        using GraphicsPath path = RoundedPath(bounds, radius);
        using var brush = new SolidBrush(fill);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.FillPath(brush, path);
    }

    public static GraphicsPath RoundedPath(Rectangle bounds, int radius)
    {
        int diameter = Math.Max(1, radius * 2);
        var path = new GraphicsPath();

        if (diameter >= bounds.Width || diameter >= bounds.Height)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
