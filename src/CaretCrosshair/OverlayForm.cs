using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CaretCrosshair.Interop;

namespace CaretCrosshair;

/// <summary>
/// How the crosshair line is drawn. Both styles combine black and white --
/// the two luminance extremes -- so no single background color can make the
/// line fully disappear (true per-pixel inversion of the content underneath
/// isn't achievable for an overlay window: DWM composites each top-level
/// window independently, with no blend mode that reaches into other
/// windows' content).
/// </summary>
internal enum CrosshairLineStyle
{
    /// <summary>1px line, alternating black/white dashes along its length ("marching ants").</summary>
    Dashed,

    /// <summary>2px line: one solid black 1px line directly next to one solid white 1px line.</summary>
    Solid,

    /// <summary>3px line: 1px white core with a 1px black outline on each side.</summary>
    Outline,

    /// <summary>1px line, alternating single black/white pixels along its length.</summary>
    Dotted,

    /// <summary>1px line, alternating single light-gray/dark-gray pixels -- subtler than Dotted, still readable everywhere.</summary>
    DottedGray,
}

/// <summary>
/// Full-virtual-desktop, transparent, click-through, non-activating,
/// always-on-top window that paints the two crosshair lines.
/// </summary>
internal sealed class OverlayForm : Form
{
    private static readonly Color TransparentKey = Color.FromArgb(1, 1, 1);
    private const int DashLengthPx = 4;

    // Symmetric-ish around mid-gray (128): far enough apart to stay
    // distinguishable from each other and from most backgrounds, without
    // the full black/white contrast of Dotted.
    private static readonly Color LightGray = Color.FromArgb(214, 214, 214);
    private static readonly Color DarkGray = Color.FromArgb(74, 74, 74);

    private Point? _crosshairPoint;
    private Rectangle _monitorBounds;
    private CrosshairLineStyle _style = CrosshairLineStyle.Dashed;

    public CrosshairLineStyle Style
    {
        get => _style;
        set
        {
            if (_style == value)
            {
                return;
            }

            _style = value;
            Invalidate();
        }
    }

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

        switch (_style)
        {
            case CrosshairLineStyle.Dashed:
                DrawDashed(e.Graphics, monitor, localX, localY);
                break;
            case CrosshairLineStyle.Solid:
                DrawSolid(e.Graphics, monitor, localX, localY);
                break;
            case CrosshairLineStyle.Outline:
                DrawOutline(e.Graphics, monitor, localX, localY);
                break;
            case CrosshairLineStyle.Dotted:
                DrawAlternating(e.Graphics, monitor, localX, localY, 1, Color.Black, Color.White);
                break;
            case CrosshairLineStyle.DottedGray:
                DrawAlternating(e.Graphics, monitor, localX, localY, 1, DarkGray, LightGray);
                break;
        }
    }

    private static void DrawDashed(Graphics g, Rectangle monitor, int localX, int localY)
        => DrawAlternating(g, monitor, localX, localY, DashLengthPx, Color.Black, Color.White);

    /// <summary>
    /// Two 1px pens sharing one dash pattern, phase-shifted by exactly one
    /// dash length so colorA's "off" gaps are exactly where colorB's "on"
    /// dashes land -- together they tile the line with no gaps/overlaps, at
    /// a true 1px thickness. dashLength=1 alternates individual pixels
    /// ("dotted"); larger values give longer dashes ("dashed").
    /// </summary>
    private static void DrawAlternating(
        Graphics g, Rectangle monitor, int localX, int localY, int dashLength, Color colorA, Color colorB)
    {
        float[] pattern = { dashLength, dashLength };

        using var penA = new Pen(colorA, 1) { DashStyle = DashStyle.Custom, DashPattern = pattern };
        using var penB = new Pen(colorB, 1) { DashStyle = DashStyle.Custom, DashPattern = pattern, DashOffset = dashLength };

        g.DrawLine(penA, monitor.Left, localY, monitor.Right, localY);
        g.DrawLine(penB, monitor.Left, localY, monitor.Right, localY);
        g.DrawLine(penA, localX, monitor.Top, localX, monitor.Bottom);
        g.DrawLine(penB, localX, monitor.Top, localX, monitor.Bottom);
    }

    private static void DrawSolid(Graphics g, Rectangle monitor, int localX, int localY)
    {
        using var blackPen = new Pen(Color.Black, 1);
        using var whitePen = new Pen(Color.White, 1);

        g.DrawLine(blackPen, monitor.Left, localY, monitor.Right, localY);
        g.DrawLine(whitePen, monitor.Left, localY + 1, monitor.Right, localY + 1);
        g.DrawLine(blackPen, localX, monitor.Top, localX, monitor.Bottom);
        g.DrawLine(whitePen, localX + 1, monitor.Top, localX + 1, monitor.Bottom);
    }

    private static void DrawOutline(Graphics g, Rectangle monitor, int localX, int localY)
    {
        using var outlinePen = new Pen(Color.Black, 3);
        using var corePen = new Pen(Color.White, 1);

        g.DrawLine(outlinePen, monitor.Left, localY, monitor.Right, localY);
        g.DrawLine(outlinePen, localX, monitor.Top, localX, monitor.Bottom);
        g.DrawLine(corePen, monitor.Left, localY, monitor.Right, localY);
        g.DrawLine(corePen, localX, monitor.Top, localX, monitor.Bottom);
    }
}
