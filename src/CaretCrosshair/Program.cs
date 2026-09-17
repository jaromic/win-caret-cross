using System.Threading;
using System.Windows.Forms;

namespace CaretCrosshair;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var singleInstanceMutex = new Mutex(initiallyOwned: true, "CaretCrosshair.SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            // Already running -- a second launch (e.g. double-clicking the exe again) is a no-op,
            // since a duplicate instance would install duplicate global hooks.
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayAppContext());
    }
}
