using System.Text.Json;

namespace Montray;

internal sealed class UserSensorSelectionStore
{
    private readonly string _settingsPath;

    public UserSensorSelectionStore()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "montray");
        Directory.CreateDirectory(directory);
        _settingsPath = Path.Combine(directory, "settings.json");
    }

    public UserSensorSelection Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return UserSensorSelection.Empty(hasSavedMainSensors: false, hasSavedTraySensor: false);
        }

        try
        {
            using var stream = File.OpenRead(_settingsPath);
            var settings = JsonSerializer.Deserialize<UserSensorSelectionDto>(stream);

            return new UserSensorSelection(
                new HashSet<string>(settings?.MainSensorKeys ?? [], StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(settings?.WidgetSensorKeys ?? [], StringComparer.OrdinalIgnoreCase),
                settings?.TraySensorKey,
                HasSavedMainSensors: true,
                HasSavedTraySensor: !string.IsNullOrWhiteSpace(settings?.TraySensorKey),
                WidgetLocation: CreatePoint(settings?.WidgetX, settings?.WidgetY),
                WidgetAlwaysOnTop: settings?.WidgetAlwaysOnTop ?? true,
                WidgetOpacity: Math.Clamp(settings?.WidgetOpacity ?? 0.96, 0.35, 1.0),
                WidgetShowSparkline: settings?.WidgetShowSparkline ?? true,
                Thresholds: new TemperatureThresholds(
                    settings?.WarmThreshold ?? TemperatureThresholds.Default.Warm,
                    settings?.HotThreshold ?? TemperatureThresholds.Default.Hot,
                    settings?.CriticalThreshold ?? TemperatureThresholds.Default.Critical).Normalize());
        }
        catch (JsonException)
        {
            return UserSensorSelection.Empty(hasSavedMainSensors: false, hasSavedTraySensor: false);
        }
        catch (IOException)
        {
            return UserSensorSelection.Empty(hasSavedMainSensors: false, hasSavedTraySensor: false);
        }
    }

    public void Save(UserSensorSelection selection)
    {
        var settings = new UserSensorSelectionDto
        {
            MainSensorKeys = selection.MainSensorKeys.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            WidgetSensorKeys = selection.WidgetSensorKeys.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            TraySensorKey = selection.TraySensorKey,
            WidgetX = selection.WidgetLocation?.X,
            WidgetY = selection.WidgetLocation?.Y,
            WidgetAlwaysOnTop = selection.WidgetAlwaysOnTop,
            WidgetOpacity = selection.WidgetOpacity,
            WidgetShowSparkline = selection.WidgetShowSparkline,
            WarmThreshold = selection.Thresholds.Warm,
            HotThreshold = selection.Thresholds.Hot,
            CriticalThreshold = selection.Thresholds.Critical
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        using var stream = File.Create(_settingsPath);
        JsonSerializer.Serialize(stream, settings, options);
    }

    private static Point? CreatePoint(int? x, int? y)
    {
        return x.HasValue && y.HasValue
            ? new Point(x.Value, y.Value)
            : null;
    }

    private sealed class UserSensorSelectionDto
    {
        public string[] MainSensorKeys { get; set; } = [];

        public string[] WidgetSensorKeys { get; set; } = [];

        public string? TraySensorKey { get; set; }

        public int? WidgetX { get; set; }

        public int? WidgetY { get; set; }

        public bool? WidgetAlwaysOnTop { get; set; }

        public double? WidgetOpacity { get; set; }

        public bool? WidgetShowSparkline { get; set; }

        public float? WarmThreshold { get; set; }

        public float? HotThreshold { get; set; }

        public float? CriticalThreshold { get; set; }
    }
}

internal sealed record UserSensorSelection(
    HashSet<string> MainSensorKeys,
    HashSet<string> WidgetSensorKeys,
    string? TraySensorKey,
    bool HasSavedMainSensors,
    bool HasSavedTraySensor,
    Point? WidgetLocation,
    bool WidgetAlwaysOnTop,
    double WidgetOpacity,
    bool WidgetShowSparkline,
    TemperatureThresholds Thresholds)
{
    public static UserSensorSelection Empty(bool hasSavedMainSensors, bool hasSavedTraySensor)
    {
        return new UserSensorSelection(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            TraySensorKey: null,
            hasSavedMainSensors,
            hasSavedTraySensor,
            WidgetLocation: null,
            WidgetAlwaysOnTop: true,
            WidgetOpacity: 0.96,
            WidgetShowSparkline: true,
            Thresholds: TemperatureThresholds.Default);
    }
}
