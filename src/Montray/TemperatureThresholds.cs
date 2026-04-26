namespace Montray;

internal sealed record TemperatureThresholds(float Warm, float Hot, float Critical)
{
    public static TemperatureThresholds Default { get; } = new(60f, 75f, 90f);

    public TemperatureThresholds Normalize()
    {
        var warm = Math.Clamp(Warm, 1f, 150f);
        var hot = Math.Clamp(MathF.Max(Hot, warm + 1f), 2f, 160f);
        var critical = Math.Clamp(MathF.Max(Critical, hot + 1f), 3f, 170f);
        return new TemperatureThresholds(warm, hot, critical);
    }
}
