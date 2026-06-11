using Microsoft.Extensions.Logging;
using Montray.Hardware;

namespace Montray;

internal static class Program
{
    private const string SingleInstanceMutexName = @"Local\Montray.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var singleInstanceMutex = new Mutex(
            initiallyOwned: true,
            name: SingleInstanceMutexName,
            createdNew: out var createdNew);

        if (!createdNew)
        {
            MessageBox.Show(
                "montray is already running.",
                "montray",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        var logger = loggerFactory.CreateLogger<TrayApplicationContext>();

        using var hardwareMonitor = new HardwareMonitorService();
        Application.Run(new TrayApplicationContext(hardwareMonitor, logger));
    }
}
