namespace Montray.Core;

public static class TrayTooltipFormatter
{
    public const int NotifyIconTextLimit = 63;

    public static string FormatSummary(
        IEnumerable<SensorReading> readings,
        int maxLength = NotifyIconTextLimit)
    {
        var temperatureReadings = readings
            .Where(reading => reading.SensorType.Equals("Temperature", StringComparison.OrdinalIgnoreCase))
            .Where(reading => reading.Unit.Equals("C", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var cpu = SelectPreferredTemperature(temperatureReadings, HardwareCategory.Cpu);
        var gpu = SelectPreferredTemperature(temperatureReadings, HardwareCategory.Gpu);

        var summary = $"montray | CPU {FormatTemperature(cpu)} | GPU {FormatTemperature(gpu)}";
        return Truncate(summary, maxLength);
    }

    private static SensorReading? SelectPreferredTemperature(
        IEnumerable<SensorReading> readings,
        HardwareCategory category)
    {
        return readings
            .Where(reading => reading.Category == category && reading.Value.HasValue)
            .OrderByDescending(ScoreTemperatureSensor)
            .ThenBy(reading => reading.SensorName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static int ScoreTemperatureSensor(SensorReading reading)
    {
        var name = reading.SensorName;

        if (name.Contains("package", StringComparison.OrdinalIgnoreCase))
        {
            return 40;
        }

        if (name.Contains("hot spot", StringComparison.OrdinalIgnoreCase))
        {
            return 35;
        }

        if (name.Contains("core", StringComparison.OrdinalIgnoreCase))
        {
            return 30;
        }

        if (name.Contains("die", StringComparison.OrdinalIgnoreCase))
        {
            return 20;
        }

        return 0;
    }

    private static string FormatTemperature(SensorReading? reading)
    {
        return reading?.Value is { } value
            ? $"{MathF.Round(value)} C"
            : "N/A";
    }

    private static string Truncate(string text, int maxLength)
    {
        if (maxLength <= 3)
        {
            return text[..Math.Min(text.Length, maxLength)];
        }

        return text.Length <= maxLength
            ? text
            : string.Concat(text.AsSpan(0, maxLength - 3), "...");
    }
}
