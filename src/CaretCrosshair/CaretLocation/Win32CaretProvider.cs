using System.Drawing;
using CaretCrosshair.Interop;

namespace CaretCrosshair.CaretLocation;

/// <summary>
/// Fallback caret provider using the classic Win32 caret API
/// (GetGUIThreadInfo). Covers legacy Win32 edit/rich-edit controls that
/// don't expose UI Automation's TextPattern2.
/// </summary>
internal sealed class Win32CaretProvider : ICaretProvider
{
    public bool TryGetCaretPosition(out Point screenPosition)
    {
        screenPosition = default;

        var info = new GUITHREADINFO
        {
            cbSize = System.Runtime.InteropServices.Marshal.SizeOf<GUITHREADINFO>(),
        };

        // idThread = 0 asks for the info of the current foreground thread.
        if (!NativeMethods.GetGUIThreadInfo(0, ref info))
        {
            return false;
        }

        if (info.hwndCaret == 0)
        {
            // No window currently owns a caret -- nothing to show.
            return false;
        }

        var topLeft = new Point(info.rcCaret.Left, info.rcCaret.Top);
        if (!NativeMethods.ClientToScreen(info.hwndCaret, ref topLeft))
        {
            return false;
        }

        // Bottom edge of the caret, not its vertical center -- matches UiaCaretProvider.
        int height = info.rcCaret.Bottom - info.rcCaret.Top;
        screenPosition = new Point(topLeft.X, topLeft.Y + height);
        return true;
    }
}
