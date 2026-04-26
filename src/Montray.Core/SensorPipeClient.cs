using System.IO.Pipes;
using System.Text.Json;

namespace Montray.Core;

public sealed class SensorPipeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<SensorReading>?> GetReadingsAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync("readings", timeout, cancellationToken);
    }

    public async Task<IReadOnlyList<SensorReading>?> RefreshAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync("refresh", timeout, cancellationToken);
    }

    private static async Task<IReadOnlyList<SensorReading>?> SendAsync(
        string command,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            await using var pipe = new NamedPipeClientStream(
                ".",
                SensorServiceConstants.PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            await pipe.ConnectAsync(timeoutSource.Token);

            await using var writer = new StreamWriter(pipe, leaveOpen: true)
            {
                AutoFlush = true
            };
            using var reader = new StreamReader(pipe, leaveOpen: true);

            await writer.WriteLineAsync(command.AsMemory(), timeoutSource.Token);
            var response = await reader.ReadLineAsync(timeoutSource.Token);
            if (string.IsNullOrWhiteSpace(response))
            {
                return null;
            }

            return JsonSerializer.Deserialize<SensorReading[]>(response, JsonOptions);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
