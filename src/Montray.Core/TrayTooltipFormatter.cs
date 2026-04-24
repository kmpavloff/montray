namespace Montray.Core;

public static class TrayTooltipFormatter
{
    public const int NotifyIconTextLimit = 63;

    public static string FormatSummary(
        IEnumerable<SensorReading> readings,
        int maxLength = NotifyIconTextLimit)
    {
        var materialized = readings as SensorReading[] ?? readings.ToArray();
        var cpu = TemperatureReadingSelector.SelectPreferredTemperature(materialized, HardwareCategory.Cpu);
        var gpu = TemperatureReadingSelector.SelectPreferredTemperature(materialized, HardwareCategory.Gpu);

        var summary = $"montray | CPU {FormatTemperature(cpu)} | GPU {FormatTemperature(gpu)}";
        return Truncate(summary, maxLength);
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
