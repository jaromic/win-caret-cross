namespace CaretCrosshair.Interop;

/// <summary>
/// Passive, non-blocking WH_KEYBOARD_LL hook that fires once per Alt key-down
/// transition (not on OS auto-repeat). Always calls CallNextHookEx so it can
/// never swallow or alter keyboard input.
/// </summary>
internal sealed class AltKeyHook : IDisposable
{
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private nint _hookHandle;
    private bool _altCurrentlyDown;

    public event Action? AltPressed;

    public AltKeyHook()
    {
        // Keep a strong reference to the delegate for the lifetime of the hook;
        // otherwise the GC can collect it while the hook is still installed.
        _proc = HookCallback;
    }

    public void Install()
    {
        if (_hookHandle != 0)
        {
            return;
        }

        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        nint moduleHandle = NativeMethods.GetModuleHandleW(curModule.ModuleName);

        _hookHandle = NativeMethods.SetWindowsHookExW(
            NativeMethods.WH_KEYBOARD_LL, _proc, moduleHandle, 0);
    }

    public void Uninstall()
    {
        if (_hookHandle != 0)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = 0;
        }
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var data = System.Runtime.InteropServices.Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            bool isAltKey = data.vkCode == NativeMethods.VK_LMENU
                || data.vkCode == NativeMethods.VK_RMENU
                || data.vkCode == NativeMethods.VK_MENU;

            if (isAltKey)
            {
                int msg = (int)wParam;
                if (msg is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN)
                {
                    if (!_altCurrentlyDown)
                    {
                        _altCurrentlyDown = true;
                        AltPressed?.Invoke();
                    }
                }
                else if (msg is NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP)
                {
                    _altCurrentlyDown = false;
                }
            }
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose() => Uninstall();
}
