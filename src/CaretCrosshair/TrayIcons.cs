using System.Drawing;
using CaretCrosshair.Interop;

namespace CaretCrosshair;

/// <summary>
/// Small crosshair-shaped tray icons drawn at runtime, so the portable
/// build doesn't need to ship separate .ico assets.
/// </summary>
internal static class TrayIcons
{
    public static Icon Default { get; } = Build(Color.Red);
    public static Icon Disabled { get; } = Build(Color.Gray);

    private static Icon Build(Color color)
    {
        using var bitmap = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            using var pen = new Pen(color, 2);
            g.DrawLine(pen, 8, 0, 8, 15);
            g.DrawLine(pen, 0, 8, 15, 8);
        }

        nint hIcon = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone(); // Clone duplicates the GDI resource, so the original HICON can be destroyed below.
        }
        finally
        {
            NativeMethods.DestroyIcon(hIcon);
        }
    }
}
