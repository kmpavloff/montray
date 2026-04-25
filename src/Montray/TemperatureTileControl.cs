using Montray.Core;

namespace Montray;

internal sealed class TemperatureTileControl : Control
{
    private SensorReading? _reading;
    private IReadOnlyList<float> _history = Array.Empty<float>();

    public TemperatureTileControl()
    {
        DoubleBuffered = true;
        MinimumSize = new Size(150, 92);
        Padding = new Padding(12, 10, 12, 10);
        BackColor = Color.FromArgb(32, 37, 43);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9f);
        Margin = new Padding(0, 0, 10, 10);
    }

    public void SetReading(SensorReading reading, IReadOnlyList<float> history)
    {
        _reading = reading;
        _history = history;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var bounds = ClientRectangle;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        using var background = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(background, bounds);

        DrawHistory(e.Graphics, bounds);
        DrawText(e.Graphics, bounds);
    }

    private void DrawHistory(Graphics graphics, Rectangle bounds)
    {
        if (_history.Count < 2)
        {
            return;
        }

        var min = MathF.Min(_history.Min(), 35f);
        var max = MathF.Max(_history.Max(), 90f);
        var range = MathF.Max(1f, max - min);
        var graph = Rectangle.Inflate(bounds, -8, -8);
        graph.Y += 18;
        graph.Height -= 20;

        var points = _history
            .Select((value, index) =>
            {
                var x = graph.Left + (graph.Width * index / Math.Max(1, _history.Count - 1));
                var y = graph.Bottom - ((value - min) / range * graph.Height);
                return new PointF(x, y);
            })
            .ToArray();

        using var fill = new SolidBrush(Color.FromArgb(22, SelectTemperatureColor(_history[^1])));
        using var pen = new Pen(Color.FromArgb(180, SelectTemperatureColor(_history[^1])), 2f);
        using var gridPen = new Pen(Color.FromArgb(36, 255, 255, 255), 1f);

        graphics.DrawLine(gridPen, graph.Left, graph.Bottom, graph.Right, graph.Bottom);
        if (points.Length > 1)
        {
            var area = points
                .Concat([new PointF(graph.Right, graph.Bottom), new PointF(graph.Left, graph.Bottom)])
                .ToArray();
            graphics.FillPolygon(fill, area);
            graphics.DrawLines(pen, points);
        }
    }

    private void DrawText(Graphics graphics, Rectangle bounds)
    {
        if (_reading is null)
        {
            return;
        }

        var title = SensorReadingIdentity.CreateTitle(_reading);
        var subtitle = SensorReadingIdentity.CreateSubtitle(_reading);
        var valueText = _reading.Value is { } value ? $"{MathF.Round(value)}°C" : "N/A";

        using var titleBrush = new SolidBrush(Color.FromArgb(185, 195, 207));
        using var valueBrush = new SolidBrush(Color.White);
        using var subtitleBrush = new SolidBrush(Color.FromArgb(150, 160, 172));
        using var titleFont = new Font(Font, FontStyle.Bold);
        using var valueFont = new Font("Segoe UI", 19f, FontStyle.Bold);
        using var subtitleFont = new Font("Segoe UI", 8f);

        graphics.DrawString(title, titleFont, titleBrush, Padding.Left, Padding.Top);

        var valueSize = graphics.MeasureString(valueText, valueFont);
        graphics.DrawString(
            valueText,
            valueFont,
            valueBrush,
            bounds.Right - Padding.Right - valueSize.Width,
            Padding.Top + 14);

        var subtitleBounds = new RectangleF(
            Padding.Left,
            bounds.Bottom - Padding.Bottom - 20,
            bounds.Width - Padding.Horizontal,
            18);
        using var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
        graphics.DrawString(subtitle, subtitleFont, subtitleBrush, subtitleBounds, format);
    }

    private static Color SelectTemperatureColor(float temperature)
    {
        return temperature switch
        {
            >= 90 => Color.FromArgb(220, 64, 64),
            >= 75 => Color.FromArgb(228, 118, 49),
            >= 60 => Color.FromArgb(218, 166, 51),
            _ => Color.FromArgb(50, 168, 113)
        };
    }
}
