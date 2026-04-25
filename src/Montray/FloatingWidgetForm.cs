using System.Runtime.InteropServices;
using Montray.Core;

namespace Montray;

internal sealed class FloatingWidgetForm : Form
{
    private const int WmNclButtonDown = 0xA1;
    private const int HtCaption = 0x2;

    private readonly FlowLayoutPanel _panel;
    private readonly ContextMenuStrip _contextMenu;
    private readonly Dictionary<string, TemperatureTileControl> _tiles = new(StringComparer.OrdinalIgnoreCase);

    public FloatingWidgetForm(
        Action hideWidget,
        Action showDetails,
        Action exitApplication)
    {
        Text = "montray widget";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Color.FromArgb(20, 23, 28);
        ForeColor = Color.White;
        Size = new Size(340, 220);
        MinimumSize = Size;
        MaximumSize = Size;
        Opacity = 0.96;

        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        Location = new Point(workingArea.Right - Width - 16, workingArea.Bottom - Height - 16);

        _panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            BackColor = BackColor,
            AutoScroll = true,
            WrapContents = true
        };

        Controls.Add(_panel);
        MouseDown += BeginDrag;
        _panel.MouseDown += BeginDrag;

        _contextMenu = BuildContextMenu(hideWidget, showDetails, exitApplication);
        ContextMenuStrip = _contextMenu;
        _panel.ContextMenuStrip = _contextMenu;
    }

    public void SetReadings(
        IReadOnlyList<SensorReading> readings,
        IReadOnlySet<string> widgetSensorKeys,
        SensorHistoryStore history)
    {
        var selected = SelectWidgetReadings(readings, widgetSensorKeys);
        UpdateTiles(selected, history);
    }

    public void ShowError(string message)
    {
        Text = $"montray widget | {message}";
    }

    private static IReadOnlyList<SensorReading> SelectWidgetReadings(
        IReadOnlyList<SensorReading> readings,
        IReadOnlySet<string> widgetSensorKeys)
    {
        var temperatures = TemperatureReadingSelector.SelectDisplayTemperatures(readings);
        if (widgetSensorKeys.Count > 0)
        {
            return temperatures
                .Where(reading => widgetSensorKeys.Contains(SensorReadingIdentity.CreateKey(reading)))
                .ToArray();
        }

        return TemperatureReadingSelector.SelectTemperatureSummaries(readings)
            .Select(summary => summary.Reading)
            .OfType<SensorReading>()
            .Take(4)
            .ToArray();
    }

    private void UpdateTiles(IReadOnlyList<SensorReading> readings, SensorHistoryStore history)
    {
        _panel.SuspendLayout();

        foreach (var key in _tiles.Keys.Except(readings.Select(SensorReadingIdentity.CreateKey)).ToArray())
        {
            var tile = _tiles[key];
            _panel.Controls.Remove(tile);
            tile.Dispose();
            _tiles.Remove(key);
        }

        foreach (var reading in readings)
        {
            var key = SensorReadingIdentity.CreateKey(reading);
            if (!_tiles.TryGetValue(key, out var tile))
            {
                tile = new TemperatureTileControl { Size = new Size(150, 92) };
                tile.MouseDown += BeginDrag;
                tile.ContextMenuStrip = _contextMenu;
                _tiles.Add(key, tile);
                _panel.Controls.Add(tile);
            }

            tile.SetReading(reading, history.GetSamples(key));
        }

        _panel.ResumeLayout();
    }

    private static ContextMenuStrip BuildContextMenu(
        Action hideWidget,
        Action showDetails,
        Action exitApplication)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Hide widget", null, (_, _) => hideWidget());
        menu.Items.Add("Show details", null, (_, _) => showDetails());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => exitApplication());
        return menu;
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
}
