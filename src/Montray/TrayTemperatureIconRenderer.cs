using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using Montray.Core;

namespace Montray;

internal static class TrayTemperatureIconRenderer
{
    public static Icon Render(SensorReading? reading, TemperatureThresholds thresholds)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            graphics.Clear(Color.Transparent);

            DrawTemperature(graphics, new Rectangle(0, 0, 32, 32), reading, thresholds.Normalize());

            using var borderPen = new Pen(Color.FromArgb(80, Color.Black), 1);
            graphics.DrawRectangle(borderPen, 0, 0, 31, 31);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static void DrawTemperature(
        Graphics graphics,
        Rectangle bounds,
        SensorReading? reading,
        TemperatureThresholds thresholds)
    {
        var text = FormatIconText(reading);
        using var backgroundBrush = new SolidBrush(SelectBackgroundColor(reading?.Value, thresholds));
        graphics.FillRectangle(backgroundBrush, bounds);

        using var labelFont = new Font("Segoe UI", 7f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var valueFont = CreateValueFont(text);
        using var textBrush = new SolidBrush(Color.White);
        using var valueFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap
        };

        if (reading is not null)
        {
            using var labelFormat = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near,
                FormatFlags = StringFormatFlags.NoWrap
            };
            graphics.DrawString(
                SensorReadingIdentity.CreateTitle(reading)[0].ToString(),
                labelFont,
                textBrush,
                new RectangleF(2, 1, 10, 8),
                labelFormat);
        }

        graphics.DrawString(text, valueFont, textBrush, bounds, valueFormat);
    }

    private static string FormatIconText(SensorReading? reading)
    {
        return reading?.Value is { } value
            ? Math.Clamp((int)MathF.Round(value), 0, 999).ToString()
            : "NA";
    }

    private static Font CreateValueFont(string text)
    {
        var size = text.Length switch
        {
            <= 2 => 21f,
            3 => 17f,
            _ => 12f
        };

        return new Font("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Pixel);
    }

    private static Color SelectBackgroundColor(float? temperature, TemperatureThresholds thresholds)
    {
        if (temperature is null)
        {
            return Color.FromArgb(82, 91, 102);
        }

        return temperature.Value switch
        {
            var value when value >= thresholds.Critical => Color.FromArgb(188, 42, 42),
            var value when value >= thresholds.Hot => Color.FromArgb(210, 95, 38),
            var value when value >= thresholds.Warm => Color.FromArgb(196, 145, 35),
            _ => Color.FromArgb(35, 126, 89)
        };
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
