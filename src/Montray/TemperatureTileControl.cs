using Montray.Core;

namespace Montray;

internal sealed class TemperatureTileControl : Control
{
    private SensorReading? _reading;
    private IReadOnlyList<float> _history = Array.Empty<float>();
    private TemperatureThresholds _thresholds = TemperatureThresholds.Default;
    private bool _isCompact;

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

    public bool IsCompact
    {
        get => _isCompact;
        set
        {
            if (_isCompact == value)
            {
                return;
            }

            _isCompact = value;
            MinimumSize = value ? new Size(104, 46) : new Size(150, 92);
            Padding = value ? new Padding(9, 6, 9, 6) : new Padding(12, 10, 12, 10);
            Invalidate();
        }
    }

    public bool ShowHistory { get; set; } = true;

    public TemperatureThresholds Thresholds
    {
        get => _thresholds;
        set
        {
            _thresholds = value.Normalize();
            Invalidate();
        }
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

        if (IsCompact)
        {
            DrawCompact(e.Graphics, bounds);
            return;
        }

        if (ShowHistory)
        {
            DrawHistory(e.Graphics, bounds);
        }

        DrawExpandedText(e.Graphics, bounds);
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

        using var fill = new SolidBrush(Color.FromArgb(22, SelectTemperatureColor(_history[^1], Thresholds)));
        using var pen = new Pen(Color.FromArgb(180, SelectTemperatureColor(_history[^1], Thresholds)), 2f);
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

    private void DrawExpandedText(Graphics graphics, Rectangle bounds)
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

    private void DrawCompact(Graphics graphics, Rectangle bounds)
    {
        if (_reading is null)
        {
            return;
        }

        var temperature = _reading.Value;
        var title = CreateCompactTitle(_reading);
        var valueText = temperature is { } value ? $"{MathF.Round(value)}°" : "N/A";
        var accentColor = temperature is { } current
            ? SelectTemperatureColor(current, Thresholds)
            : Color.FromArgb(120, 130, 142);

        using var accentBrush = new SolidBrush(accentColor);
        graphics.FillRectangle(accentBrush, new Rectangle(0, 0, 4, bounds.Height));

        if (ShowHistory)
        {
            DrawCompactHistory(graphics, bounds, accentColor);
        }

        using var titleBrush = new SolidBrush(Color.FromArgb(185, 195, 207));
        using var valueBrush = new SolidBrush(Color.White);
        using var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var valueFont = new Font("Segoe UI", 33f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var titleFormat = new StringFormat
        {
            Trimming = StringTrimming.None,
            FormatFlags = StringFormatFlags.NoWrap,
            LineAlignment = StringAlignment.Near
        };
        using var valueFormat = new StringFormat
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Near,
            FormatFlags = StringFormatFlags.NoWrap
        };

        var titleBounds = new RectangleF(
            Padding.Left,
            4,
            bounds.Width - Padding.Horizontal,
            12);
        var valueBounds = new RectangleF(
            Padding.Left,
            7,
            bounds.Width - Padding.Horizontal,
            36);

        graphics.DrawString(title, titleFont, titleBrush, titleBounds, titleFormat);
        graphics.DrawString(valueText, valueFont, valueBrush, valueBounds, valueFormat);
    }

    private void DrawCompactHistory(Graphics graphics, Rectangle bounds, Color color)
    {
        if (_history.Count < 2)
        {
            return;
        }

        var min = MathF.Min(_history.Min(), 35f);
        var max = MathF.Max(_history.Max(), 90f);
        var range = MathF.Max(1f, max - min);
        var graph = new Rectangle(
            Padding.Left,
            bounds.Bottom - Padding.Bottom - 5,
            bounds.Width - Padding.Horizontal,
            5);

        var points = _history
            .Select((value, index) =>
            {
                var x = graph.Left + (graph.Width * index / Math.Max(1, _history.Count - 1));
                var y = graph.Bottom - ((value - min) / range * graph.Height);
                return new PointF(x, y);
            })
            .ToArray();

        using var pen = new Pen(Color.FromArgb(170, color), 1.5f);
        graphics.DrawLines(pen, points);
    }

    private static string CreateCompactTitle(SensorReading reading)
    {
        return reading.Category switch
        {
            HardwareCategory.Cpu => "CPU",
            HardwareCategory.Gpu => "GPU",
            HardwareCategory.Memory => "RAM",
            HardwareCategory.Storage => "SSD",
            HardwareCategory.Motherboard => "MB",
            _ => SensorReadingIdentity.CreateTitle(reading)
        };
    }

    private static Color SelectTemperatureColor(float temperature, TemperatureThresholds thresholds)
    {
        return temperature switch
        {
            var value when value >= thresholds.Critical => Color.FromArgb(220, 64, 64),
            var value when value >= thresholds.Hot => Color.FromArgb(228, 118, 49),
            var value when value >= thresholds.Warm => Color.FromArgb(218, 166, 51),
            _ => Color.FromArgb(50, 168, 113)
        };
    }
}
