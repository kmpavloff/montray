using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using Montray.Core;

namespace Montray;

public static class TrayTemperatureIconRenderer
{
    public static Icon Render(IReadOnlyList<SensorReading> readings)
    {
        var cpu = TemperatureReadingSelector.SelectPreferredTemperature(readings, HardwareCategory.Cpu);
        var gpu = TemperatureReadingSelector.SelectPreferredTemperature(readings, HardwareCategory.Gpu);

        using var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            graphics.Clear(Color.Transparent);

            DrawTemperatureRow(graphics, new Rectangle(0, 0, 32, 16), "C", cpu);
            DrawTemperatureRow(graphics, new Rectangle(0, 16, 32, 16), "G", gpu);

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

    private static void DrawTemperatureRow(
        Graphics graphics,
        Rectangle bounds,
        string label,
        SensorReading? reading)
    {
        var text = FormatIconText(reading);
        using var backgroundBrush = new SolidBrush(SelectBackgroundColor(reading?.Value));
        graphics.FillRectangle(backgroundBrush, bounds);

        using var labelFont = new Font("Segoe UI", 7f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var valueFont = CreateValueFont(text);
        using var textBrush = new SolidBrush(Color.White);
        using var labelFormat = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap
        };
        using var valueFormat = new StringFormat
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap
        };

        graphics.DrawString(label, labelFont, textBrush, new RectangleF(1, bounds.Y, 8, bounds.Height), labelFormat);
        graphics.DrawString(text, valueFont, textBrush, new RectangleF(8, bounds.Y, 23, bounds.Height), valueFormat);
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
            <= 2 => 11f,
            3 => 9f,
            _ => 8f
        };

        return new Font("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Pixel);
    }

    private static Color SelectBackgroundColor(float? temperature)
    {
        if (temperature is null)
        {
            return Color.FromArgb(82, 91, 102);
        }

        return temperature.Value switch
        {
            >= 90 => Color.FromArgb(188, 42, 42),
            >= 75 => Color.FromArgb(210, 95, 38),
            >= 60 => Color.FromArgb(196, 145, 35),
            _ => Color.FromArgb(35, 126, 89)
        };
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
