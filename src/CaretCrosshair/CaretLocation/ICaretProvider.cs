using System.Drawing;

namespace CaretCrosshair.CaretLocation;

internal interface ICaretProvider
{
    /// <summary>
    /// Attempts to find the currently visible text caret's screen position.
    /// Returns false if none can be reliably determined -- callers must not
    /// guess or fall back to a stale position in that case.
    /// </summary>
    bool TryGetCaretPosition(out Point screenPosition);
}
