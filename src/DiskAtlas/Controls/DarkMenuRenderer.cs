namespace DiskAtlas.Controls;

/// <summary>
/// Colours for the context menus. A ContextMenuStrip ignores BackColor for most of what
/// it draws, so the palette has to come through a colour table instead.
/// </summary>
internal sealed class DarkMenuColors : ProfessionalColorTable
{
    public DarkMenuColors() => UseSystemColors = false;

    public override Color ToolStripDropDownBackground => Theme.SurfaceRaised;

    public override Color ImageMarginGradientBegin => Theme.SurfaceRaised;

    public override Color ImageMarginGradientMiddle => Theme.SurfaceRaised;

    public override Color ImageMarginGradientEnd => Theme.SurfaceRaised;

    public override Color MenuBorder => Theme.Border;

    public override Color MenuItemBorder => Theme.Accent;

    public override Color MenuItemSelected => Theme.SurfaceHover;

    public override Color MenuItemSelectedGradientBegin => Theme.SurfaceHover;

    public override Color MenuItemSelectedGradientEnd => Theme.SurfaceHover;

    public override Color MenuItemPressedGradientBegin => Theme.SurfaceHover;

    public override Color MenuItemPressedGradientEnd => Theme.SurfaceHover;

    public override Color SeparatorDark => Theme.Border;

    public override Color SeparatorLight => Theme.Border;
}

/// <summary>
/// Draws the menu with the dark palette, and keeps disabled entries readable rather than
/// letting the system grey them into the background.
/// </summary>
internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer()
        : base(new DarkMenuColors())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item switch
        {
            { Enabled: false } => Theme.TextMuted,
            _ => Theme.TextPrimary,
        };

        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        Rectangle bounds = e.Item.ContentRectangle;
        using var pen = new Pen(Theme.Border);
        int middle = bounds.Top + (bounds.Height / 2);
        e.Graphics.DrawLine(pen, bounds.Left + 8, middle, bounds.Right - 8, middle);
    }
}
