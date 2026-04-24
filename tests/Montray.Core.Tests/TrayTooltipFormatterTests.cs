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
}
