using System.Diagnostics;
using System.Reflection;

namespace DiskAtlas;

public partial class AboutForm : Form
{
    /// <summary>Where the project lives. Shown in the dialog and opened when clicked.</summary>
    private const string RepositoryUrl = "https://github.com/szabolevi98/disk-atlas";

    public AboutForm()
    {
        InitializeComponent();

        Font = Theme.UiFont;
        titleLabel.Font = new Font("Segoe UI Light", 17F);
        versionLabel.Font = Theme.CaptionFont;
        licenseLabel.Font = Theme.CaptionFont;
        repositoryLink.Font = Theme.CaptionFont;
        copyrightLabel.Font = Theme.UiFont;
        closeButton.FlatAppearance.BorderColor = Theme.Border;
        closeButton.FlatAppearance.MouseOverBackColor = Theme.SurfaceHover;

        versionLabel.Text = $"Version {ReadVersion()}";
        LoadIcon();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Native.NativeMethods.UseDarkTitleBar(Handle);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Native.NativeMethods.UseDarkTitleBar(Handle);
    }

    /// <summary>
    /// Reads the version the assembly was built with, so the dialog cannot drift away
    /// from what was actually shipped.
    /// </summary>
    private static string ReadVersion()
    {
        Assembly assembly = typeof(AboutForm).Assembly;

        string? informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            // Strip the source revision the SDK appends after a plus sign.
            int plus = informational.IndexOf('+');
            return plus < 0 ? informational : informational[..plus];
        }

        return assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    }

    private void LoadIcon()
    {
        using Stream? stream = typeof(AboutForm).Assembly.GetManifestResourceStream("DiskAtlas.app.ico");
        if (stream is null)
        {
            return;
        }

        // Asking for the size the box shows picks the variant drawn for it rather than
        // scaling the largest one down.
        using var icon = new Icon(stream, iconBox.Width, iconBox.Height);
        iconBox.Image = icon.ToBitmap();
    }

    private void RepositoryLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(RepositoryUrl) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // No handler for the address, or the user dismissed the prompt.
        }
    }
}
