using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using DiskAtlas.Model;

namespace DiskAtlas.Rendering;

/// <summary>One drawn rectangle, kept so the mouse can be mapped back to a file.</summary>
internal sealed record TreemapItem(DiskNode Node, Rectangle Bounds);

/// <summary>
/// A finished treemap: the painted bitmap plus a per pixel index into the item list, so
/// hit testing costs one array lookup no matter how many rectangles were drawn.
/// </summary>
internal sealed class TreemapRender : IDisposable
{
    private readonly int[] _hitMap;
    private readonly int _width;

    internal TreemapRender(Bitmap image, List<TreemapItem> items, int[] hitMap, int width)
    {
        Image = image;
        Items = items;
        _hitMap = hitMap;
        _width = width;
    }

    public Bitmap Image { get; }

    public IReadOnlyList<TreemapItem> Items { get; }

    public TreemapItem? HitTest(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Image.Width || y >= Image.Height)
        {
            return null;
        }

        int index = _hitMap[(y * _width) + x];
        return index >= 0 && index < Items.Count ? Items[index] : null;
    }

    public void Dispose() => Image.Dispose();
}

/// <summary>
/// Squarified treemap with cushion shading. The layout keeps rectangles close to square
/// so areas stay comparable, and the shading is the parabolic surface from the cushion
/// treemap paper, which is what gives this kind of map its recognisable soft relief.
/// </summary>
internal static class TreemapRenderer
{
    /// <summary>Rectangles below this many pixels are not worth subdividing further.</summary>
    private const int MinimumSubdivisionArea = 180;

    /// <summary>How much each nesting level bulges. Higher values give deeper grooves.</summary>
    private const double CushionHeight = 0.40;

    /// <summary>Each level contributes less than the one above it.</summary>
    private const double CushionFalloff = 0.80;

    private const double AmbientLight = 0.46;

    /// <summary>How far the one pixel groove around each rectangle is darkened.</summary>
    private const double EdgeShade = 0.62;

    // A light sitting up and to the left, normalised.
    private const double LightX = -0.1032;
    private const double LightY = -0.2064;
    private const double LightZ = 0.9731;

    public static TreemapRender Render(DiskNode root, int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        List<TreemapItem> items = [];
        int[] hitMap = new int[width * height];
        Array.Fill(hitMap, -1);

        int[] pixels = new int[width * height];
        int empty = Theme.Background.ToArgb();
        Array.Fill(pixels, empty);

        if (root.SizeOnDisk > 0)
        {
            var surface = new Cushion();
            Layout(root, new RectangleF(0, 0, width, height), surface, 0, pixels, hitMap, width, height, items);
        }

        BitmapData data = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppPArgb);

        try
        {
            Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return new TreemapRender(bitmap, items, hitMap, width);
    }

    /// <summary>
    /// The accumulated parabolic surface. Every nesting level adds a ridge, which is what
    /// makes nested folders look like stacked pillows rather than a flat mosaic.
    /// </summary>
    private struct Cushion
    {
        public double Ax;
        public double Bx;
        public double Ay;
        public double By;

        public readonly Cushion WithRidge(RectangleF bounds, double height)
        {
            Cushion next = this;
            AddRidge(bounds.Left, bounds.Right, height, ref next.Ax, ref next.Bx);
            AddRidge(bounds.Top, bounds.Bottom, height, ref next.Ay, ref next.By);
            return next;
        }

        private static void AddRidge(double from, double to, double height, ref double a, ref double b)
        {
            double size = to - from;
            if (size <= 0)
            {
                return;
            }

            b += 4 * height * (to + from) / size;
            a -= 4 * height / size;
        }
    }

    private static void Layout(
        DiskNode node,
        RectangleF bounds,
        Cushion surface,
        int depth,
        int[] pixels,
        int[] hitMap,
        int width,
        int height,
        List<TreemapItem> items)
    {
        if (bounds.Width < 1 || bounds.Height < 1)
        {
            return;
        }

        bool subdivide = node.IsDirectory
            && node.HasChildren
            && bounds.Width * bounds.Height >= MinimumSubdivisionArea;

        if (!subdivide)
        {
            Paint(node, bounds, surface, depth, pixels, hitMap, width, height, items);
            return;
        }

        Cushion nested = surface.WithRidge(bounds, CushionHeight * Math.Pow(CushionFalloff, depth));
        Squarify(node, bounds, nested, depth, pixels, hitMap, width, height, items);
    }

    /// <summary>
    /// Places children in rows along whichever side is shorter, extending a row while it
    /// keeps the rectangles closer to square, which is the squarified treemap heuristic.
    /// </summary>
    private static void Squarify(
        DiskNode parent,
        RectangleF bounds,
        Cushion surface,
        int depth,
        int[] pixels,
        int[] hitMap,
        int width,
        int height,
        List<TreemapItem> items)
    {
        IReadOnlyList<DiskNode> children = parent.Children;
        long total = parent.SizeOnDisk;
        if (total <= 0)
        {
            return;
        }

        RectangleF remaining = bounds;
        double remainingSize = total;
        int index = 0;

        while (index < children.Count && remaining.Width >= 1 && remaining.Height >= 1)
        {
            // Children are already sorted largest first, so anything from here on is noise.
            if (children[index].SizeOnDisk <= 0)
            {
                break;
            }

            bool horizontal = remaining.Width >= remaining.Height;
            double shortSide = horizontal ? remaining.Height : remaining.Width;
            double area = (double)remaining.Width * remaining.Height;

            int rowEnd = index;
            double rowSize = 0;
            double worst = double.MaxValue;

            while (rowEnd < children.Count)
            {
                long childSize = children[rowEnd].SizeOnDisk;
                if (childSize <= 0)
                {
                    break;
                }

                double candidateSize = rowSize + childSize;
                double candidateWorst = WorstAspect(
                    children, index, rowEnd, candidateSize, shortSide, area, remainingSize);

                if (rowEnd > index && candidateWorst > worst)
                {
                    break;
                }

                worst = candidateWorst;
                rowSize = candidateSize;
                rowEnd++;
            }

            if (rowEnd == index)
            {
                break;
            }

            // The row takes its share of the remaining area, so its thickness along the
            // long side is simply that share of the long side.
            float rowThickness = horizontal
                ? (float)(remaining.Width * (rowSize / remainingSize))
                : (float)(remaining.Height * (rowSize / remainingSize));

            float offset = 0;
            for (int i = index; i < rowEnd; i++)
            {
                double share = children[i].SizeOnDisk / rowSize;
                RectangleF cell = horizontal
                    ? new RectangleF(remaining.X, remaining.Y + offset, rowThickness, (float)(remaining.Height * share))
                    : new RectangleF(remaining.X + offset, remaining.Y, (float)(remaining.Width * share), rowThickness);

                offset += horizontal ? cell.Height : cell.Width;
                Layout(children[i], cell, surface, depth + 1, pixels, hitMap, width, height, items);
            }

            if (horizontal)
            {
                remaining = new RectangleF(
                    remaining.X + rowThickness, remaining.Y, remaining.Width - rowThickness, remaining.Height);
            }
            else
            {
                remaining = new RectangleF(
                    remaining.X, remaining.Y + rowThickness, remaining.Width, remaining.Height - rowThickness);
            }

            remainingSize -= rowSize;
            index = rowEnd;

            if (remainingSize <= 0)
            {
                break;
            }
        }
    }

    private static double WorstAspect(
        IReadOnlyList<DiskNode> children,
        int from,
        int to,
        double rowSize,
        double shortSide,
        double area,
        double remainingSize)
    {
        if (rowSize <= 0)
        {
            return double.MaxValue;
        }

        double rowArea = area * (rowSize / remainingSize);
        double rowThickness = rowArea / shortSide;
        if (rowThickness <= 0)
        {
            return double.MaxValue;
        }

        double worst = 0;
        for (int i = from; i <= to; i++)
        {
            double cellArea = area * (children[i].SizeOnDisk / remainingSize);
            double cellLength = cellArea / rowThickness;
            if (cellLength <= 0)
            {
                return double.MaxValue;
            }

            double ratio = Math.Max(rowThickness / cellLength, cellLength / rowThickness);
            worst = Math.Max(worst, ratio);
        }

        return worst;
    }

    private static void Paint(
        DiskNode node,
        RectangleF bounds,
        Cushion surface,
        int depth,
        int[] pixels,
        int[] hitMap,
        int width,
        int height,
        List<TreemapItem> items)
    {
        int left = (int)Math.Floor(bounds.Left);
        int top = (int)Math.Floor(bounds.Top);
        int right = (int)Math.Ceiling(bounds.Right);
        int bottom = (int)Math.Ceiling(bounds.Bottom);

        left = Math.Clamp(left, 0, width);
        top = Math.Clamp(top, 0, height);
        right = Math.Clamp(right, 0, width);
        bottom = Math.Clamp(bottom, 0, height);

        if (right <= left || bottom <= top)
        {
            return;
        }

        Cushion cushion = surface.WithRidge(bounds, CushionHeight * Math.Pow(CushionFalloff, depth));

        Color baseColor = node.IsDirectory
            ? FileTypeColors.Other.Color
            : FileTypeColors.Categorize(node.Name).Color;

        int itemIndex = items.Count;
        items.Add(new TreemapItem(node, Rectangle.FromLTRB(left, top, right, bottom)));

        // Neighbouring files of the same kind would melt into one another, so the outer
        // ring is darkened into a groove that separates them.
        bool outline = right - left > 2 && bottom - top > 2;

        for (int y = top; y < bottom; y++)
        {
            double ny = -((2 * cushion.Ay * (y + 0.5)) + cushion.By);
            int row = y * width;
            bool edgeRow = y == top || y == bottom - 1;

            for (int x = left; x < right; x++)
            {
                double nx = -((2 * cushion.Ax * (x + 0.5)) + cushion.Bx);
                double cosine = ((nx * LightX) + (ny * LightY) + LightZ)
                    / Math.Sqrt((nx * nx) + (ny * ny) + 1.0);

                double intensity = AmbientLight + Math.Max(0, (1 - AmbientLight) * cosine);

                if (outline && (edgeRow || x == left || x == right - 1))
                {
                    intensity *= EdgeShade;
                }

                pixels[row + x] = Shade(baseColor, intensity);
                hitMap[row + x] = itemIndex;
            }
        }
    }

    private static int Shade(Color color, double intensity)
    {
        int red = Math.Clamp((int)(color.R * intensity), 0, 255);
        int green = Math.Clamp((int)(color.G * intensity), 0, 255);
        int blue = Math.Clamp((int)(color.B * intensity), 0, 255);
        return unchecked((int)0xFF000000) | (red << 16) | (green << 8) | blue;
    }
}
