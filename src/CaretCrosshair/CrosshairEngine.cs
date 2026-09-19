using System.Drawing;
using System.Windows.Forms;
using CaretCrosshair.CaretLocation;
using CaretCrosshair.Interop;

namespace CaretCrosshair;

/// <summary>
/// Owns the enable/disable state and drives the overlay:
///   - polls the caret position and follows it while enabled
///   - on Alt key-down or any foreground-window change, flashes the
///     crosshair at the active window's top-left corner for a short hold,
///     then resumes following the caret (or hides, if none).
/// All timers run on the UI thread, matching where the keyboard/WinEvent
/// hooks are installed and pumped.
/// </summary>
internal sealed class CrosshairEngine : IDisposable
{
    private const int PollIntervalMs = 30;
    private const int FlashHoldMs = 300;

    private readonly CaretLocator _caretLocator = new();
    private readonly AltKeyHook _altKeyHook = new();
    private readonly ForegroundWatcher _foregroundWatcher = new();
    private readonly OverlayForm _overlay = new();
    private readonly System.Windows.Forms.Timer _pollTimer;
    private readonly System.Windows.Forms.Timer _flashTimer;

    // Windows reserves a z-order band above ordinary topmost windows for shell
    // chrome (Start menu, search flyout) -- there's no public API for a normal
    // window to draw above it. Rather than render incorrectly underneath it, we
    // hide the crosshair while one of these is the foreground window.
    private static readonly HashSet<string> ShellSurfaceProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "StartMenuExperienceHost",
        "SearchHost",
    };

    private bool _enabled = true;
    private bool _flashing;
    private bool _suppressedByShellSurface;

    public event Action<bool>? EnabledChanged;

    public bool IsEnabled => _enabled;

    public CrosshairLineStyle LineStyle
    {
        get => _overlay.Style;
        set => _overlay.Style = value;
    }

    public CrosshairEngine()
    {
        _pollTimer = new System.Windows.Forms.Timer { Interval = PollIntervalMs };
        _pollTimer.Tick += (_, _) => Poll();

        _flashTimer = new System.Windows.Forms.Timer { Interval = FlashHoldMs };
        _flashTimer.Tick += (_, _) => EndFlash();

        _altKeyHook.AltPressed += OnAltPressed;
        _foregroundWatcher.ForegroundChanged += OnForegroundChanged;
    }

    public void Start()
    {
        _altKeyHook.Install();
        _foregroundWatcher.Install();
        _suppressedByShellSurface = IsShellSurface(NativeMethods.GetForegroundWindow());
        _pollTimer.Start();
    }

    public void SetEnabled(bool enabled)
    {
        if (_enabled == enabled)
        {
            return;
        }

        _enabled = enabled;

        if (!_enabled)
        {
            _flashTimer.Stop();
            _flashing = false;
            _overlay.HideCrosshair();
        }

        EnabledChanged?.Invoke(_enabled);
    }

    public void Toggle() => SetEnabled(!_enabled);

    private void Poll()
    {
        if (!_enabled || _flashing)
        {
            return;
        }

        if (_suppressedByShellSurface)
        {
            _overlay.HideCrosshair();
            return;
        }

        if (_caretLocator.TryGetCaretPosition(out Point caretPoint))
        {
            _overlay.ShowCrosshairAt(caretPoint);
        }
        else
        {
            _overlay.HideCrosshair();
        }
    }

    private void OnAltPressed()
    {
        nint hwnd = NativeMethods.GetForegroundWindow();
        _suppressedByShellSurface = IsShellSurface(hwnd);
        FlashActiveWindowCorner(hwnd);
    }

    private void OnForegroundChanged(nint hwnd)
    {
        _suppressedByShellSurface = IsShellSurface(hwnd);
        FlashActiveWindowCorner(hwnd);
    }

    private void FlashActiveWindowCorner(nint hwnd)
    {
        if (!_enabled || hwnd == 0)
        {
            return;
        }

        if (_suppressedByShellSurface)
        {
            _overlay.HideCrosshair();
            return;
        }

        if (!NativeMethods.GetWindowRect(hwnd, out RECT rect))
        {
            return;
        }

        _flashing = true;
        _overlay.ShowCrosshairAt(new Point(rect.Left, rect.Top));
        _flashTimer.Stop();
        _flashTimer.Start();
    }

    private static bool IsShellSurface(nint hwnd)
    {
        if (hwnd == 0)
        {
            return false;
        }

        try
        {
            NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0)
            {
                return false;
            }

            using var process = System.Diagnostics.Process.GetProcessById((int)pid);
            return ShellSurfaceProcessNames.Contains(process.ProcessName);
        }
        catch (ArgumentException)
        {
            // Process already exited between GetWindowThreadProcessId and GetProcessById.
            return false;
        }
    }

    private void EndFlash()
    {
        _flashTimer.Stop();
        _flashing = false;
        // Immediately re-evaluate against the caret instead of waiting for the next poll tick.
        Poll();
    }

    public void Dispose()
    {
        _pollTimer.Dispose();
        _flashTimer.Dispose();
        _altKeyHook.Dispose();
        _foregroundWatcher.Dispose();
        _overlay.Dispose();
    }
}
