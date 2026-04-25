using Montray.Core;

namespace Montray;

internal sealed class SensorHistoryStore
{
    private const int MaxSamples = 180;
    private readonly Dictionary<string, Queue<float>> _samples = new(StringComparer.OrdinalIgnoreCase);

    public void AddReadings(IEnumerable<SensorReading> readings)
    {
        foreach (var reading in TemperatureReadingSelector.SelectDisplayTemperatures(readings))
        {
            if (reading.Value is not { } value)
            {
                continue;
            }

            var key = SensorReadingIdentity.CreateKey(reading);
            if (!_samples.TryGetValue(key, out var values))
            {
                values = new Queue<float>(MaxSamples);
                _samples.Add(key, values);
            }

            values.Enqueue(value);
            while (values.Count > MaxSamples)
            {
                values.Dequeue();
            }
        }
    }

    public IReadOnlyList<float> GetSamples(string key)
    {
        return _samples.TryGetValue(key, out var values)
            ? values.ToArray()
            : Array.Empty<float>();
    }
}
