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
            return UserSensorSelection.Empty(hasSavedMainSensors: false);
        }

        try
        {
            using var stream = File.OpenRead(_settingsPath);
            var settings = JsonSerializer.Deserialize<UserSensorSelectionDto>(stream);

            return new UserSensorSelection(
                new HashSet<string>(settings?.MainSensorKeys ?? [], StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(settings?.WidgetSensorKeys ?? [], StringComparer.OrdinalIgnoreCase),
                HasSavedMainSensors: true);
        }
        catch (JsonException)
        {
            return UserSensorSelection.Empty(hasSavedMainSensors: false);
        }
        catch (IOException)
        {
            return UserSensorSelection.Empty(hasSavedMainSensors: false);
        }
    }

    public void Save(IReadOnlyCollection<string> mainSensorKeys, IReadOnlyCollection<string> widgetSensorKeys)
    {
        var settings = new UserSensorSelectionDto
        {
            MainSensorKeys = mainSensorKeys.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            WidgetSensorKeys = widgetSensorKeys.Order(StringComparer.OrdinalIgnoreCase).ToArray()
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        using var stream = File.Create(_settingsPath);
        JsonSerializer.Serialize(stream, settings, options);
    }

    private sealed class UserSensorSelectionDto
    {
        public string[] MainSensorKeys { get; set; } = [];

        public string[] WidgetSensorKeys { get; set; } = [];
    }
}

internal sealed record UserSensorSelection(
    HashSet<string> MainSensorKeys,
    HashSet<string> WidgetSensorKeys,
    bool HasSavedMainSensors)
{
    public static UserSensorSelection Empty(bool hasSavedMainSensors)
    {
        return new UserSensorSelection(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            hasSavedMainSensors);
    }
}
