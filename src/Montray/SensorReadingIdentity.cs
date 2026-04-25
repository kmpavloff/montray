using Montray.Core;

namespace Montray;

internal static class SensorReadingIdentity
{
    public static string CreateKey(SensorReading reading)
    {
        return string.Join(
            "|",
            reading.Category,
            reading.HardwareName,
            reading.SensorName,
            reading.SensorType,
            reading.Unit);
    }

    public static string CreateTitle(SensorReading reading)
    {
        return reading.Category switch
        {
            HardwareCategory.Cpu => "CPU",
            HardwareCategory.Gpu => "GPU",
            HardwareCategory.Memory => "RAM",
            HardwareCategory.Storage => "SSD",
            HardwareCategory.Motherboard => "Board",
            _ => reading.Category.ToString()
        };
    }

    public static string CreateSubtitle(SensorReading reading)
    {
        return $"{reading.HardwareName} / {reading.SensorName}";
    }
}
