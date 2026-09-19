using System.Windows.Forms;
using CaretCrosshair.Interop;

namespace CaretCrosshair;

/// <summary>
/// Tray-only application shell: no visible main window, just a NotifyIcon
/// with Enable/Disable/Exit, plus the global Ctrl+Alt+X toggle hotkey.
/// </summary>
internal sealed class TrayAppContext : ApplicationContext
{
    private readonly CrosshairEngine _engine = new();
    private readonly HotkeyWindow _hotkeyWindow = new();
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _enabledMenuItem;
    private readonly ToolStripMenuItem _dashedStyleMenuItem;
    private readonly ToolStripMenuItem _solidStyleMenuItem;
    private readonly ToolStripMenuItem _outlineStyleMenuItem;

    public TrayAppContext()
    {
        _enabledMenuItem = new ToolStripMenuItem("Enabled", null, OnToggleClicked) { Checked = true };

        _dashedStyleMenuItem = new ToolStripMenuItem("Dashed (1px)", null, (_, _) => SetLineStyle(CrosshairLineStyle.Dashed));
        _solidStyleMenuItem = new ToolStripMenuItem("Solid (2px)", null, (_, _) => SetLineStyle(CrosshairLineStyle.Solid));
        _outlineStyleMenuItem = new ToolStripMenuItem("Outline (3px)", null, (_, _) => SetLineStyle(CrosshairLineStyle.Outline));
        var lineStyleMenu = new ToolStripMenuItem("Crosshair Style");
        lineStyleMenu.DropDownItems.Add(_dashedStyleMenuItem);
        lineStyleMenu.DropDownItems.Add(_solidStyleMenuItem);
        lineStyleMenu.DropDownItems.Add(_outlineStyleMenuItem);

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
        _dashedStyleMenuItem.Checked = _engine.LineStyle == CrosshairLineStyle.Dashed;
        _solidStyleMenuItem.Checked = _engine.LineStyle == CrosshairLineStyle.Solid;
        _outlineStyleMenuItem.Checked = _engine.LineStyle == CrosshairLineStyle.Outline;
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
