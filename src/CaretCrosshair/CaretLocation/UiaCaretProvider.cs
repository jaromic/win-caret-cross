using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using Point = System.Drawing.Point;

namespace CaretCrosshair.CaretLocation;

/// <summary>
/// Primary caret provider, built on UI Automation's TextPattern.
///
/// .NET's managed UI Automation client (System.Windows.Automation) predates
/// TextPattern2's GetCaretRange -- that would require hand-written COM
/// interop against IUIAutomationTextPattern2, which isn't something we can
/// verify without a Windows machine. Instead we use the well-established
/// technique of reading TextPattern.GetSelection(): with nothing selected
/// (the common case while typing or navigating with arrow keys) the
/// selection is a zero-length range sitting exactly at the caret, so its
/// bounding rectangle *is* the caret's screen position. If text is actively
/// selected, this reports the selection's bounds instead of a literal caret
/// line -- see README known limitations.
/// </summary>
internal sealed class UiaCaretProvider : ICaretProvider
{
    public bool TryGetCaretPosition(out Point screenPosition)
    {
        screenPosition = default;

        try
        {
            AutomationElement? focused = AutomationElement.FocusedElement;
            if (focused == null)
            {
                return false;
            }

            if (!focused.TryGetCurrentPattern(TextPattern.Pattern, out object patternObj))
            {
                return false;
            }

            var textPattern = (TextPattern)patternObj;
            TextPatternRange[] selection = textPattern.GetSelection();
            if (selection.Length == 0)
            {
                return false;
            }

            Rect[] rects = selection[0].GetBoundingRectangles();
            if (rects.Length == 0)
            {
                return false;
            }

            Rect rect = rects[0];
            if (rect.IsEmpty || double.IsNaN(rect.X) || double.IsNaN(rect.Y))
            {
                return false;
            }

            // Bottom edge of the caret, not its vertical center -- reads more
            // naturally as "under the caret" than bisecting it.
            screenPosition = new Point(
                (int)Math.Round(rect.X),
                (int)Math.Round(rect.Bottom));
            return true;
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return false;
        }
    }
}
