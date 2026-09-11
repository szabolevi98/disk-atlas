namespace DiskAtlas.Rendering;

/// <summary>
/// Maps file extensions to colours so the treemap groups related files visually. The
/// palette is deliberately small: a legend with fifty entries tells nobody anything.
/// </summary>
internal static class FileTypeColors
{
    public sealed record Category(string Name, Color Color, string[] Extensions);

    public static readonly Category Video = new("Video", Color.FromArgb(0xF5, 0x9E, 0x0B),
        [".mkv", ".mp4", ".avi", ".mov", ".wmv", ".m4v", ".webm", ".mpg", ".mpeg", ".flv", ".ts"]);

    public static readonly Category Images = new("Images", Color.FromArgb(0xA7, 0x8B, 0xFA),
        [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tif", ".tiff", ".psd", ".raw", ".heic", ".svg", ".ico"]);

    public static readonly Category Audio = new("Audio", Color.FromArgb(0xF4, 0x72, 0xB6),
        [".mp3", ".flac", ".wav", ".aac", ".ogg", ".m4a", ".wma", ".opus"]);

    public static readonly Category Archives = new("Archives", Color.FromArgb(0xFB, 0x71, 0x85),
        [".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso", ".cab", ".msi", ".vhd", ".vhdx"]);

    public static readonly Category Programs = new("Programs", Color.FromArgb(0x38, 0xBD, 0xF8),
        [".exe", ".dll", ".sys", ".so", ".dylib", ".ocx", ".drv", ".pyd", ".node", ".wasm"]);

    public static readonly Category Code = new("Code", Color.FromArgb(0x2D, 0xD4, 0xBF),
        [".cs", ".c", ".h", ".cpp", ".hpp", ".js", ".ts", ".jsx", ".tsx", ".py", ".java", ".go", ".rs",
         ".php", ".rb", ".html", ".css", ".scss", ".json", ".xml", ".yml", ".yaml", ".sql", ".sh", ".ps1"]);

    public static readonly Category Documents = new("Documents", Color.FromArgb(0x60, 0xA5, 0xFA),
        [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".md", ".rtf", ".odt", ".epub", ".csv"]);

    public static readonly Category Data = new("Data", Color.FromArgb(0x34, 0xD3, 0x99),
        [".db", ".sqlite", ".mdb", ".bak", ".dat", ".bin", ".pak", ".cache", ".idx", ".pdb", ".log"]);

    public static readonly Category Other = new("Other", Color.FromArgb(0x64, 0x74, 0x8B), []);

    public static readonly Category Free = new("Free space", Color.FromArgb(0x1B, 0x24, 0x32), []);

    /// <summary>Categories in legend order, the free space marker excluded.</summary>
    public static readonly IReadOnlyList<Category> All =
        [Video, Images, Audio, Archives, Programs, Code, Documents, Data, Other];

    private static readonly Dictionary<string, Category> Lookup = BuildLookup();

    private static Dictionary<string, Category> BuildLookup()
    {
        Dictionary<string, Category> lookup = new(StringComparer.OrdinalIgnoreCase);

        foreach (Category category in All)
        {
            foreach (string extension in category.Extensions)
            {
                lookup[extension] = category;
            }
        }

        return lookup;
    }

    public static Category Categorize(string fileName)
    {
        int dot = fileName.LastIndexOf('.');
        if (dot < 0 || dot == fileName.Length - 1)
        {
            return Other;
        }

        return Lookup.GetValueOrDefault(fileName[dot..], Other);
    }
}
