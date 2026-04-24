using System.Runtime.InteropServices;
using Montray.Core;

namespace Montray;

public sealed class FloatingWidgetForm : Form
{
    private const int WmNclButtonDown = 0xA1;
    private const int HtCaption = 0x2;

    private readonly TableLayoutPanel _grid;
    private readonly Dictionary<string, MetricTile> _tiles = new(StringComparer.OrdinalIgnoreCase);

    public FloatingWidgetForm()
    {
        Text = "montray widget";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Color.FromArgb(20, 23, 28);
        ForeColor = Color.White;
        Size = new Size(280, 190);
        MinimumSize = Size;
        MaximumSize = Size;
        Opacity = 0.96;

        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        Location = new Point(workingArea.Right - Width - 16, workingArea.Bottom - Height - 16);

        _grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(10),
            BackColor = BackColor
        };
        _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        Controls.Add(_grid);
        MouseDown += BeginDrag;
        _grid.MouseDown += BeginDrag;

        AddTile("CPU", 0, 0);
        AddTile("GPU", 1, 0);
        AddTile("RAM", 0, 1);
        AddTile("SSD", 1, 1);
    }

    public void SetReadings(IReadOnlyList<SensorReading> readings)
    {
        var summaries = TemperatureReadingSelector.SelectTemperatureSummaries(readings)
            .Where(summary => _tiles.ContainsKey(summary.Component))
            .ToDictionary(summary => summary.Component, StringComparer.OrdinalIgnoreCase);

        foreach (var (component, tile) in _tiles)
        {
            summaries.TryGetValue(component, out var summary);
            tile.SetReading(summary?.Reading);
        }
    }

    public void ShowError(string message)
    {
        foreach (var tile in _tiles.Values)
        {
            tile.SetError();
        }

        Text = $"montray widget | {message}";
    }

    private void AddTile(string component, int column, int row)
    {
        var tile = new MetricTile(component)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4)
        };
        tile.MouseDown += BeginDrag;
        tile.ForwardMouseDownTo(BeginDrag);

        _tiles.Add(component, tile);
        _grid.Controls.Add(tile, column, row);
    }

    private void BeginDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        ReleaseCapture();
        SendMessage(Handle, WmNclButtonDown, HtCaption, 0);
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    private sealed class MetricTile : Panel
    {
        private readonly Label _componentLabel;
        private readonly Label _valueLabel;
        private readonly Panel _statusBar;

        public MetricTile(string component)
        {
            BackColor = Color.FromArgb(34, 39, 46);
            Padding = new Padding(8, 6, 8, 0);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = BackColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 4));

            _componentLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = component,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(178, 187, 198),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                AutoSize = false,
                BackColor = BackColor,
                Margin = Padding.Empty
            };

            _valueLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "N/A",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                AutoSize = false,
                BackColor = BackColor,
                Margin = Padding.Empty
            };

            _statusBar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(82, 91, 102),
                Margin = Padding.Empty
            };

            layout.Controls.Add(_componentLabel, 0, 0);
            layout.Controls.Add(_valueLabel, 0, 1);
            layout.Controls.Add(_statusBar, 0, 2);
            Controls.Add(layout);

            layout.MouseDown += (_, e) => OnMouseDown(e);
        }

        public void SetReading(SensorReading? reading)
        {
            if (reading?.Value is not { } value)
            {
                _valueLabel.Text = "N/A";
                _statusBar.BackColor = Color.FromArgb(82, 91, 102);
                return;
            }

            _valueLabel.Text = $"{MathF.Round(value)}°C";
            _statusBar.BackColor = SelectTemperatureColor(value);
        }

        public void SetError()
        {
            _valueLabel.Text = "ERR";
            _statusBar.BackColor = Color.FromArgb(188, 42, 42);
        }

        public void ForwardMouseDownTo(MouseEventHandler handler)
        {
            foreach (Control control in Controls)
            {
                control.MouseDown += handler;

                foreach (Control child in control.Controls)
                {
                    child.MouseDown += handler;
                }
            }
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
}
