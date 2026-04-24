namespace Montray.Core;

public static class TemperatureReadingSelector
{
    private static readonly IReadOnlyList<(string Component, HardwareCategory Category, bool IsOptional)> SummaryCategories =
    [
        ("CPU", HardwareCategory.Cpu, false),
        ("GPU", HardwareCategory.Gpu, false),
        ("RAM", HardwareCategory.Memory, false),
        ("SSD", HardwareCategory.Storage, false),
        ("Motherboard", HardwareCategory.Motherboard, true)
    ];

    public static SensorReading? SelectPrimaryTemperature(IEnumerable<SensorReading> readings)
    {
        return SelectPreferredTemperatures(readings)
            .OrderByDescending(reading => reading.Value)
            .ThenBy(reading => reading.Category)
            .FirstOrDefault();
    }

    public static SensorReading? SelectPreferredTemperature(
        IEnumerable<SensorReading> readings,
        HardwareCategory category)
    {
        var materialized = readings as SensorReading[] ?? readings.ToArray();
        var directReading = materialized
            .Where(IsCurrentCelsiusTemperature)
            .Where(reading => reading.Category == category && reading.Value.HasValue)
            .OrderByDescending(ScoreTemperatureSensor)
            .ThenBy(reading => reading.SensorName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (directReading is not null || category != HardwareCategory.Cpu)
        {
            return directReading;
        }

        return materialized
            .Where(IsCurrentCelsiusTemperature)
            .Where(reading => reading.Value.HasValue)
            .Where(IsCpuFallbackCandidate)
            .OrderByDescending(ScoreTemperatureSensor)
            .ThenBy(reading => reading.HardwareName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(reading => reading.SensorName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public static IReadOnlyList<SensorReading> SelectDisplayTemperatures(IEnumerable<SensorReading> readings)
    {
        var materialized = readings as SensorReading[] ?? readings.ToArray();
        return materialized
            .Where(IsCurrentCelsiusTemperature)
            .Where(reading => reading.Value.HasValue)
            .OrderBy(reading => reading.Category)
            .ThenByDescending(ScoreTemperatureSensor)
            .ThenBy(reading => reading.HardwareName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(reading => reading.SensorName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<TemperatureSummary> SelectTemperatureSummaries(IEnumerable<SensorReading> readings)
    {
        var materialized = readings as SensorReading[] ?? readings.ToArray();

        return SummaryCategories
            .Select(item => new TemperatureSummary(
                item.Component,
                SelectSummaryTemperature(materialized, item.Category),
                item.IsOptional))
            .Where(summary => !summary.IsOptional || summary.Reading is not null)
            .ToArray();
    }

    private static IEnumerable<SensorReading> SelectPreferredTemperatures(IEnumerable<SensorReading> readings)
    {
        var materialized = readings as SensorReading[] ?? readings.ToArray();
        var cpu = SelectPreferredTemperature(materialized, HardwareCategory.Cpu);
        var gpu = SelectPreferredTemperature(materialized, HardwareCategory.Gpu);

        if (cpu is not null)
        {
            yield return cpu;
        }

        if (gpu is not null)
        {
            yield return gpu;
        }
    }

    private static SensorReading? SelectSummaryTemperature(
        IEnumerable<SensorReading> readings,
        HardwareCategory category)
    {
        return category is HardwareCategory.Cpu or HardwareCategory.Gpu
            ? SelectPreferredTemperature(readings, category)
            : SelectHottestTemperature(readings, category);
    }

    private static SensorReading? SelectHottestTemperature(
        IEnumerable<SensorReading> readings,
        HardwareCategory category)
    {
        return readings
            .Where(IsCurrentCelsiusTemperature)
            .Where(reading => reading.Category == category && reading.Value.HasValue)
            .OrderByDescending(reading => reading.Value)
            .ThenByDescending(ScoreTemperatureSensor)
            .ThenBy(reading => reading.HardwareName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(reading => reading.SensorName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static bool IsCelsiusTemperature(SensorReading reading)
    {
        return reading.SensorType.Equals("Temperature", StringComparison.OrdinalIgnoreCase)
            && reading.Unit.Equals("C", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCurrentCelsiusTemperature(SensorReading reading)
    {
        return IsCelsiusTemperature(reading) && !IsLimitTemperature(reading);
    }

    private static bool IsLimitTemperature(SensorReading reading)
    {
        var searchableName = string.Concat(reading.HardwareName, " ", reading.SensorName);

        return searchableName.Contains("critical", StringComparison.OrdinalIgnoreCase)
            || searchableName.Contains("warning", StringComparison.OrdinalIgnoreCase)
            || searchableName.Contains("limit", StringComparison.OrdinalIgnoreCase)
            || searchableName.Contains("threshold", StringComparison.OrdinalIgnoreCase)
            || searchableName.Contains("tjmax", StringComparison.OrdinalIgnoreCase)
            || searchableName.Contains("tjunction", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCpuFallbackCandidate(SensorReading reading)
    {
        if (reading.Category == HardwareCategory.Gpu)
        {
            return false;
        }

        var searchableName = string.Concat(reading.HardwareName, " ", reading.SensorName);

        return searchableName.Contains("cpu", StringComparison.OrdinalIgnoreCase)
            || searchableName.Contains("package", StringComparison.OrdinalIgnoreCase)
            || searchableName.Contains("tctl", StringComparison.OrdinalIgnoreCase)
            || searchableName.Contains("tdie", StringComparison.OrdinalIgnoreCase)
            || searchableName.Contains("ccd", StringComparison.OrdinalIgnoreCase);
    }

    private static int ScoreTemperatureSensor(SensorReading reading)
    {
        var name = reading.SensorName;

        if (name.Contains("tctl", StringComparison.OrdinalIgnoreCase)
            || name.Contains("tdie", StringComparison.OrdinalIgnoreCase))
        {
            return 50;
        }

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
}
