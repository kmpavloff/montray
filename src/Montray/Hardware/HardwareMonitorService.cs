using LibreHardwareMonitor.Hardware;
using Montray.Core;

namespace Montray.Hardware;

public sealed class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _updateVisitor = new();
    private bool _disposed;

    public HardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true
        };

        _computer.Open();
    }

    public IReadOnlyList<SensorReading> GetReadings()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _computer.Accept(_updateVisitor);

        return _computer.Hardware
            .SelectMany(ReadHardware)
            .Where(reading => reading.Value.HasValue)
            .OrderBy(reading => reading.Category)
            .ThenBy(reading => reading.HardwareName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(reading => reading.SensorName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public void Refresh()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _computer.Close();
        _computer.Open();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _computer.Close();
        _disposed = true;
    }

    private static IEnumerable<SensorReading> ReadHardware(IHardware hardware)
    {
        foreach (var sensor in hardware.Sensors)
        {
            yield return new SensorReading(
                hardware.Name,
                sensor.Name,
                MapHardwareCategory(hardware.HardwareType),
                sensor.SensorType.ToString(),
                sensor.Value,
                MapUnit(sensor.SensorType));
        }

        foreach (var subHardware in hardware.SubHardware)
        {
            foreach (var reading in ReadHardware(subHardware))
            {
                yield return reading;
            }
        }
    }

    private static HardwareCategory MapHardwareCategory(HardwareType hardwareType)
    {
        return hardwareType.ToString() switch
        {
            "Cpu" => HardwareCategory.Cpu,
            "GpuAmd" or "GpuIntel" or "GpuNvidia" => HardwareCategory.Gpu,
            "Memory" => HardwareCategory.Memory,
            "Storage" => HardwareCategory.Storage,
            "Motherboard" or "SuperIO" => HardwareCategory.Motherboard,
            _ => HardwareCategory.Unknown
        };
    }

    private static string MapUnit(SensorType sensorType)
    {
        return sensorType.ToString() switch
        {
            "Temperature" => "C",
            "Load" => "%",
            "Control" => "%",
            "Level" => "%",
            "Fan" => "RPM",
            "Clock" => "MHz",
            "Power" => "W",
            "Voltage" => "V",
            "Data" => "GB",
            "SmallData" => "MB",
            "Throughput" => "B/s",
            "Factor" => "x",
            "Energy" => "Wh",
            "Current" => "A",
            _ => string.Empty
        };
    }

    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();

            foreach (var subHardware in hardware.SubHardware)
            {
                subHardware.Accept(this);
            }
        }

        public void VisitSensor(ISensor sensor)
        {
        }

        public void VisitParameter(IParameter parameter)
        {
        }
    }
}
