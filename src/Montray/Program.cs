using Montray.Hardware;

namespace Montray;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var hardwareMonitor = new HardwareMonitorService();
        Application.Run(new TrayApplicationContext(hardwareMonitor));
    }
}
