using System.ServiceProcess;

namespace Montray.Service;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        if (Environment.UserInteractive && args.Contains("--console", StringComparer.OrdinalIgnoreCase))
        {
            using var service = new SensorServiceHost();
            await service.RunAsync(CancellationToken.None);
            return;
        }

        ServiceBase.Run(new SensorWindowsService());
    }
}
