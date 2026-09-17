using System.Drawing;

namespace CaretCrosshair.CaretLocation;

/// <summary>
/// Tries each caret provider in priority order and returns the first
/// reliable hit. Never estimates or falls back to a guessed position --
/// if nothing is reliably detected, callers are told there is no caret.
/// </summary>
internal sealed class CaretLocator
{
    private readonly ICaretProvider[] _providers =
    {
        new UiaCaretProvider(),
        new Win32CaretProvider(),
    };

    public bool TryGetCaretPosition(out Point screenPosition)
    {
        foreach (var provider in _providers)
        {
            if (provider.TryGetCaretPosition(out screenPosition))
            {
                return true;
            }
        }

        screenPosition = default;
        return false;
    }
}
