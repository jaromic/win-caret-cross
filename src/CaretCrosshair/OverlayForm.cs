using System.Drawing;
using System.Windows.Forms;
using CaretCrosshair.Interop;

namespace CaretCrosshair;

/// <summary>
/// Full-virtual-desktop, transparent, click-through, non-activating,
/// always-on-top window that paints the two crosshair lines.
/// </summary>
internal sealed class OverlayForm : Form
{
    private static readonly Color TransparentKey = Color.FromArgb(1, 1, 1);

    // True per-pixel inversion of whatever is underneath isn't achievable for an
    // overlay window -- DWM composites every top-level window independently, with
    // no blend mode that reaches into other windows' content. A white line with a
    // black outline is the standard substitute: it stays legible against light and
    // dark backgrounds alike instead of just one fixed color.
    private static readonly Color CoreColor = Color.White;
    private static readonly Color OutlineColor = Color.Black;
    private const int CoreWidth = 1;
    private const int OutlineWidth = 3;

    private Point? _crosshairPoint;
    private Rectangle _monitorBounds;

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        TopMost = true;
        BackColor = TransparentKey;
        TransparencyKey = TransparentKey;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.SupportsTransparentBackColor,
            true);

        Bounds = SystemInformation.VirtualScreen;
    }

    /// <summary>Never let this window take input focus, even when shown.</summary>
    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_LAYERED
                | NativeMethods.WS_EX_TRANSPARENT
                | NativeMethods.WS_EX_TOOLWINDOW
                | NativeMethods.WS_EX_NOACTIVATE;
            return cp;
        }
    }

    /// <summary>Repositions to cover the current virtual desktop (e.g. after a display-settings change).</summary>
    public void RefreshVirtualDesktopBounds()
    {
        Bounds = SystemInformation.VirtualScreen;
    }

    public void ShowCrosshairAt(Point screenPoint)
    {
        _crosshairPoint = screenPoint;
        _monitorBounds = Screen.FromPoint(screenPoint).Bounds;

        if (!Visible)
        {
            NativeMethods.ShowWindow(Handle, NativeMethods.SW_SHOWNOACTIVATE);
        }

        Invalidate();
    }

    public void HideCrosshair()
    {
        _crosshairPoint = null;
        if (Visible)
        {
            Hide();
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(TransparentKey);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_crosshairPoint is not { } point)
        {
            return;
        }

        // Bounds are in window-relative coordinates.
        int localX = point.X - Left;
        int localY = point.Y - Top;
        var monitor = new Rectangle(
            _monitorBounds.Left - Left,
            _monitorBounds.Top - Top,
            _monitorBounds.Width,
            _monitorBounds.Height);

        using var outlinePen = new Pen(OutlineColor, OutlineWidth);
        using var corePen = new Pen(CoreColor, CoreWidth);

        e.Graphics.DrawLine(outlinePen, monitor.Left, localY, monitor.Right, localY);
        e.Graphics.DrawLine(outlinePen, localX, monitor.Top, localX, monitor.Bottom);
        e.Graphics.DrawLine(corePen, monitor.Left, localY, monitor.Right, localY);
        e.Graphics.DrawLine(corePen, localX, monitor.Top, localX, monitor.Bottom);
    }
}
