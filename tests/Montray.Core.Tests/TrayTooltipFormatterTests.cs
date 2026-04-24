using Montray.Core;
using Xunit;

namespace Montray.Core.Tests;

public sealed class TrayTooltipFormatterTests
{
    [Fact]
    public void FormatSummary_UsesPreferredCpuAndGpuTemperatureReadings()
    {
        var readings = new[]
        {
            new SensorReading("Ryzen 7", "CPU CCD1", HardwareCategory.Cpu, "Temperature", 61.8f, "C"),
            new SensorReading("Ryzen 7", "CPU Package", HardwareCategory.Cpu, "Temperature", 64.2f, "C"),
            new SensorReading("RTX 4070", "GPU Core", HardwareCategory.Gpu, "Temperature", 55.6f, "C")
        };

        var result = TrayTooltipFormatter.FormatSummary(readings);

        Assert.Equal("montray | CPU 64 C | GPU 56 C", result);
    }

    [Fact]
    public void FormatSummary_ShowsNotAvailableWhenTemperatureIsMissing()
    {
        var readings = new[]
        {
            new SensorReading("Ryzen 7", "CPU Total", HardwareCategory.Cpu, "Load", 12.5f, "%")
        };

        var result = TrayTooltipFormatter.FormatSummary(readings);

        Assert.Equal("montray | CPU N/A | GPU N/A", result);
    }

    [Fact]
    public void FormatSummary_DoesNotExceedNotifyIconTextLimit()
    {
        var readings = new[]
        {
            new SensorReading("Very long hardware name", "CPU Package", HardwareCategory.Cpu, "Temperature", 100.0f, "C"),
            new SensorReading("Very long graphics card name", "GPU Hot Spot", HardwareCategory.Gpu, "Temperature", 89.0f, "C")
        };

        var result = TrayTooltipFormatter.FormatSummary(readings, maxLength: 22);

        Assert.True(result.Length <= 22);
        Assert.Equal("montray | CPU 100 C...", result);
    }

    [Fact]
    public void SelectPrimaryTemperature_UsesHotterPreferredCpuOrGpuTemperature()
    {
        var readings = new[]
        {
            new SensorReading("Ryzen 7", "CPU Package", HardwareCategory.Cpu, "Temperature", 64.2f, "C"),
            new SensorReading("RTX 4070", "GPU Hot Spot", HardwareCategory.Gpu, "Temperature", 78.4f, "C"),
            new SensorReading("RTX 4070", "GPU Core", HardwareCategory.Gpu, "Temperature", 55.6f, "C")
        };

        var result = TemperatureReadingSelector.SelectPrimaryTemperature(readings);

        Assert.NotNull(result);
        Assert.Equal(HardwareCategory.Gpu, result.Category);
        Assert.Equal("GPU Hot Spot", result.SensorName);
    }

    [Fact]
    public void FormatSummary_UsesCpuFallbackTemperatureWhenCpuCategoryIsMissing()
    {
        var readings = new[]
        {
            new SensorReading("Nuvoton NCT6798D", "CPU", HardwareCategory.Motherboard, "Temperature", 58.2f, "C"),
            new SensorReading("Nuvoton NCT6798D", "System", HardwareCategory.Motherboard, "Temperature", 35.0f, "C"),
            new SensorReading("RTX 4070", "GPU Core", HardwareCategory.Gpu, "Temperature", 55.6f, "C")
        };

        var result = TrayTooltipFormatter.FormatSummary(readings);

        Assert.Equal("montray | CPU 58 C | GPU 56 C", result);
    }

    [Fact]
    public void FormatSummary_IgnoresCriticalTemperatureLimits()
    {
        var readings = new[]
        {
            new SensorReading("Memory", "Temperature", HardwareCategory.Memory, "Temperature", 44.8f, "C"),
            new SensorReading("NVMe", "Warning Temperature", HardwareCategory.Storage, "Temperature", 80.0f, "C"),
            new SensorReading("Memory", "Critical Temperature", HardwareCategory.Memory, "Temperature", 95.0f, "C"),
            new SensorReading("RTX 4070", "GPU Core", HardwareCategory.Gpu, "Temperature", 55.6f, "C")
        };

        var displayTemperatures = TemperatureReadingSelector.SelectDisplayTemperatures(readings);
        var result = TrayTooltipFormatter.FormatSummary(readings);

        Assert.DoesNotContain(displayTemperatures, reading => reading.SensorName.Contains("Critical"));
        Assert.DoesNotContain(displayTemperatures, reading => reading.SensorName.Contains("Warning"));
        Assert.Equal("montray | CPU N/A | GPU 56 C", result);
    }

    [Fact]
    public void SelectTemperatureSummaries_ReturnsSimpleComponentRows()
    {
        var readings = new[]
        {
            new SensorReading("Ryzen 7", "CPU Package", HardwareCategory.Cpu, "Temperature", 64.2f, "C"),
            new SensorReading("Ryzen 7", "Core 1", HardwareCategory.Cpu, "Temperature", 71.0f, "C"),
            new SensorReading("RTX 4070", "GPU Core", HardwareCategory.Gpu, "Temperature", 55.6f, "C"),
            new SensorReading("DIMM", "Temperature", HardwareCategory.Memory, "Temperature", 44.8f, "C"),
            new SensorReading("NVMe", "Composite", HardwareCategory.Storage, "Temperature", 49.2f, "C"),
            new SensorReading("Board", "System", HardwareCategory.Motherboard, "Temperature", 35.0f, "C")
        };

        var summaries = TemperatureReadingSelector.SelectTemperatureSummaries(readings);

        Assert.Equal(new[] { "CPU", "GPU", "RAM", "SSD", "Motherboard" }, summaries.Select(summary => summary.Component));
        Assert.Equal("CPU Package", summaries.Single(summary => summary.Component == "CPU").Reading?.SensorName);
        Assert.Equal(49.2f, summaries.Single(summary => summary.Component == "SSD").Reading?.Value);
    }
}
