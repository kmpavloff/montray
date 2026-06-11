using Microsoft.Extensions.Logging;
using System.ServiceProcess;

namespace Montray.Service;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        if (Environment.UserInteractive && args.Contains("--console", StringComparer.OrdinalIgnoreCase))
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            using var service = new SensorServiceHost(loggerFactory.CreateLogger<SensorServiceHost>());
            await service.RunAsync(CancellationToken.None);
            return;
        }

        ServiceBase.Run(new SensorWindowsService());
    }
}
