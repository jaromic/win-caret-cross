using System.Windows.Forms;

namespace CaretCrosshair.Interop;

/// <summary>
/// Invisible message-only window used solely to receive WM_HOTKEY for the
/// global Ctrl+Alt+X enable/disable toggle.
/// </summary>
internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int HotkeyId = 0xC470; // arbitrary, process-unique id

    public event Action? HotkeyPressed;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "CaretCrosshairHotkeyWindow",
            X = 0,
            Y = 0,
            Width = 0,
            Height = 0,
            Style = 0,
        });
    }

    public bool RegisterToggleHotkey(Keys key)
    {
        return NativeMethods.RegisterHotKey(
            Handle, HotkeyId, NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, (uint)key);
    }

    public void UnregisterToggleHotkey()
    {
        NativeMethods.UnregisterHotKey(Handle, HotkeyId);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke();
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        UnregisterToggleHotkey();
        DestroyHandle();
    }
}
