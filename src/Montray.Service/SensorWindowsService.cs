using System.ServiceProcess;
using Montray.Core;

namespace Montray.Service;

public sealed class SensorWindowsService : ServiceBase
{
    private CancellationTokenSource? _cancellation;
    private Task? _serviceTask;
    private SensorServiceHost? _host;

    public SensorWindowsService()
    {
        ServiceName = SensorServiceConstants.ServiceName;
        CanStop = true;
        CanPauseAndContinue = false;
        AutoLog = true;
    }

    protected override void OnStart(string[] args)
    {
        _cancellation = new CancellationTokenSource();
        _host = new SensorServiceHost();
        _serviceTask = Task.Run(() => _host.RunAsync(_cancellation.Token));
    }

    protected override void OnStop()
    {
        if (_cancellation is null)
        {
            return;
        }

        _cancellation.Cancel();

        try
        {
            _serviceTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
        }
        finally
        {
            _host?.Dispose();
            _cancellation.Dispose();
            _host = null;
            _cancellation = null;
            _serviceTask = null;
        }
    }
}
