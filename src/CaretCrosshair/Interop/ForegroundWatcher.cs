namespace CaretCrosshair.Interop;

/// <summary>
/// Watches for foreground-window activation changes (Alt+Tab, taskbar clicks,
/// Win+Tab, etc.) via a system-wide WinEvent hook.
/// </summary>
internal sealed class ForegroundWatcher : IDisposable
{
    private readonly NativeMethods.WinEventDelegate _proc;
    private nint _hookHandle;

    public event Action<nint>? ForegroundChanged;

    public ForegroundWatcher()
    {
        _proc = WinEventCallback;
    }

    public void Install()
    {
        if (_hookHandle != 0)
        {
            return;
        }

        _hookHandle = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            0, _proc, 0, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);
    }

    public void Uninstall()
    {
        if (_hookHandle != 0)
        {
            NativeMethods.UnhookWinEvent(_hookHandle);
            _hookHandle = 0;
        }
    }

    private void WinEventCallback(
        nint hWinEventHook, uint eventType, nint hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd != 0)
        {
            ForegroundChanged?.Invoke(hwnd);
        }
    }

    public void Dispose() => Uninstall();
}
