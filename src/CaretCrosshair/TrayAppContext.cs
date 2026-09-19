using System.Windows.Forms;
using CaretCrosshair.Interop;

namespace CaretCrosshair;

/// <summary>
/// Tray-only application shell: no visible main window, just a NotifyIcon
/// with Enable/Disable/Exit, plus the global Ctrl+Alt+X toggle hotkey.
/// </summary>
internal sealed class TrayAppContext : ApplicationContext
{
    private static readonly (CrosshairLineStyle Style, string Label)[] LineStyleOptions =
    {
        (CrosshairLineStyle.Dashed, "Dashed (1px)"),
        (CrosshairLineStyle.Dotted, "Dotted (1px)"),
        (CrosshairLineStyle.DottedGray, "Dotted Gray (1px)"),
        (CrosshairLineStyle.Solid, "Solid (2px)"),
        (CrosshairLineStyle.Outline, "Outline (3px)"),
    };

    private readonly CrosshairEngine _engine = new();
    private readonly HotkeyWindow _hotkeyWindow = new();
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _enabledMenuItem;
    private readonly Dictionary<CrosshairLineStyle, ToolStripMenuItem> _lineStyleMenuItems = new();

    public TrayAppContext()
    {
        _enabledMenuItem = new ToolStripMenuItem("Enabled", null, OnToggleClicked) { Checked = true };

        var lineStyleMenu = new ToolStripMenuItem("Crosshair Style");
        foreach (var (style, label) in LineStyleOptions)
        {
            var item = new ToolStripMenuItem(label, null, (_, _) => SetLineStyle(style));
            _lineStyleMenuItems[style] = item;
            lineStyleMenu.DropDownItems.Add(item);
        }

        var exitMenuItem = new ToolStripMenuItem("Exit", null, OnExitClicked);

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(_enabledMenuItem);
        contextMenu.Items.Add(lineStyleMenu);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitMenuItem);

        UpdateLineStyleChecks();

        _trayIcon = new NotifyIcon
        {
            Icon = TrayIcons.Default,
            Text = "Caret Crosshair",
            Visible = true,
            ContextMenuStrip = contextMenu,
        };

        _engine.EnabledChanged += OnEngineEnabledChanged;
        _hotkeyWindow.HotkeyPressed += () => _engine.Toggle();
        _hotkeyWindow.RegisterToggleHotkey(Keys.X);

        _engine.Start();
    }

    private void OnToggleClicked(object? sender, EventArgs e) => _engine.Toggle();

    private void SetLineStyle(CrosshairLineStyle style)
    {
        _engine.LineStyle = style;
        UpdateLineStyleChecks();
    }

    private void UpdateLineStyleChecks()
    {
        foreach (var (style, item) in _lineStyleMenuItems)
        {
            item.Checked = _engine.LineStyle == style;
        }
    }

    private void OnEngineEnabledChanged(bool enabled)
    {
        _enabledMenuItem.Checked = enabled;
        _trayIcon.Text = enabled ? "Caret Crosshair (enabled)" : "Caret Crosshair (disabled)";
        _trayIcon.Icon = enabled ? TrayIcons.Default : TrayIcons.Disabled;
    }

    private void OnExitClicked(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        _engine.Dispose();
        _hotkeyWindow.Dispose();
        Application.Exit();
    }
}
