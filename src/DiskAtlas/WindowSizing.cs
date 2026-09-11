namespace DiskAtlas;

/// <summary>
/// Works out how large the window may open. The designer size suits a desktop monitor,
/// but a 1366 by 768 laptop has less height than that, and a window taller than the
/// screen opens with its buttons under the task bar.
/// </summary>
internal static class WindowSizing
{
    /// <summary>Gap left between the window and the edges of the working area.</summary>
    public const int ScreenMargin = 32;

    /// <summary>
    /// Shrinks the wanted size until it fits the working area, without going below the
    /// minimum the window can usefully be. A screen smaller than the minimum wins nothing
    /// from shrinking further, so the minimum is the floor.
    /// </summary>
    public static Size FitWithin(Size wanted, Size minimum, Rectangle workingArea, int margin = ScreenMargin)
    {
        int availableWidth = Math.Max(minimum.Width, workingArea.Width - margin);
        int availableHeight = Math.Max(minimum.Height, workingArea.Height - margin);

        return new Size(
            Math.Min(wanted.Width, availableWidth),
            Math.Min(wanted.Height, availableHeight));
    }

    /// <summary>Centres a window of the given size inside the working area.</summary>
    public static Point CenterWithin(Size size, Rectangle workingArea) => new(
        workingArea.X + Math.Max(0, (workingArea.Width - size.Width) / 2),
        workingArea.Y + Math.Max(0, (workingArea.Height - size.Height) / 2));
}
