namespace Montray.Core;

public sealed record SensorReading(
    string HardwareName,
    string SensorName,
    HardwareCategory Category,
    string SensorType,
    float? Value,
    string Unit);
