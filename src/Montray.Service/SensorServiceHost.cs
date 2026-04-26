using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using Montray.Core;
using Montray.Hardware;

namespace Montray.Service;

public sealed class SensorServiceHost : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HardwareMonitorService _hardwareMonitor = new();
    private readonly SemaphoreSlim _hardwareAccess = new(1, 1);
    private bool _disposed;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var pipe = NamedPipeServerStreamAcl.Create(
                    SensorServiceConstants.PipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    0,
                    0,
                    CreatePipeSecurity());

                await pipe.WaitForConnectionAsync(cancellationToken);
                await HandleClientAsync(pipe, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static PipeSecurity CreatePipeSecurity()
    {
        var pipeSecurity = new PipeSecurity();
        var authenticatedUsers = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
        pipeSecurity.AddAccessRule(new PipeAccessRule(
            authenticatedUsers,
            PipeAccessRights.ReadWrite,
            AccessControlType.Allow));

        var localSystem = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
        pipeSecurity.AddAccessRule(new PipeAccessRule(
            localSystem,
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        return pipeSecurity;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _hardwareAccess.Dispose();
        _hardwareMonitor.Dispose();
        _disposed = true;
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(pipe, leaveOpen: true);
        await using var writer = new StreamWriter(pipe, leaveOpen: true)
        {
            AutoFlush = true
        };

        var command = await reader.ReadLineAsync(cancellationToken);
        var readings = await ExecuteCommandAsync(command, cancellationToken);
        var response = JsonSerializer.Serialize(readings, JsonOptions);
        await writer.WriteLineAsync(response.AsMemory(), cancellationToken);
    }

    private async Task<IReadOnlyList<SensorReading>> ExecuteCommandAsync(string? command, CancellationToken cancellationToken)
    {
        await _hardwareAccess.WaitAsync(cancellationToken);
        try
        {
            if (string.Equals(command, "refresh", StringComparison.OrdinalIgnoreCase))
            {
                _hardwareMonitor.Refresh();
            }

            return _hardwareMonitor.GetReadings();
        }
        finally
        {
            _hardwareAccess.Release();
        }
    }
}
