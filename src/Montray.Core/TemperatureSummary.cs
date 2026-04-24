namespace Montray.Core;

public sealed record TemperatureSummary(
    string Component,
    SensorReading? Reading,
    bool IsOptional = false);
