using Montray.Core;

namespace Montray;

internal sealed class DetailsForm : Form
{
    private readonly Action<string, bool> _setMainSensor;
    private readonly Action<string, bool> _setWidgetSensor;
    private readonly FlowLayoutPanel _dashboardPanel;
    private readonly DataGridView _grid;
    private readonly Label _statusLabel;
    private readonly Dictionary<string, TemperatureTileControl> _tiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _mainSensorKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _widgetSensorKeys = new(StringComparer.OrdinalIgnoreCase);
    private bool _isUpdatingGrid;

    public DetailsForm(
        Action<string, bool> setMainSensor,
        Action<string, bool> setWidgetSensor)
    {
        _setMainSensor = setMainSensor;
        _setWidgetSensor = setWidgetSensor;

        Text = "montray sensors";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(820, 520);
        MinimumSize = new Size(680, 420);
        BackColor = Color.FromArgb(24, 28, 33);
        ForeColor = Color.White;

        _statusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 12, 0),
            ForeColor = Color.FromArgb(190, 200, 212),
            BackColor = Color.FromArgb(24, 28, 33)
        };

        _dashboardPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 218,
            Padding = new Padding(12, 12, 2, 2),
            AutoScroll = true,
            WrapContents = true,
            BackColor = Color.FromArgb(24, 28, 33)
        };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.FromArgb(24, 28, 33),
            BorderStyle = BorderStyle.None,
            ReadOnly = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        ConfigureGrid();

        Controls.Add(_grid);
        Controls.Add(_dashboardPanel);
        Controls.Add(_statusLabel);
    }

    public void SetReadings(
        IReadOnlyList<SensorReading> readings,
        IReadOnlySet<string> mainSensorKeys,
        IReadOnlySet<string> widgetSensorKeys,
        SensorHistoryStore history)
    {
        _mainSensorKeys.Clear();
        _mainSensorKeys.UnionWith(mainSensorKeys);
        _widgetSensorKeys.Clear();
        _widgetSensorKeys.UnionWith(widgetSensorKeys);

        var temperatures = TemperatureReadingSelector.SelectDisplayTemperatures(readings);
        UpdateDashboard(temperatures, history);
        UpdateGrid(temperatures);

        _statusLabel.Text = temperatures.Count == 0
            ? "No temperature sensors detected."
            : $"{temperatures.Count} temperature sensors detected. Tick Main or Widget to pin a sensor.";
    }

    public void ShowError(string message)
    {
        _statusLabel.Text = $"Sensor error: {message}";
    }

    private void ConfigureGrid()
    {
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(34, 39, 46);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(34, 39, 46);
        _grid.EnableHeadersVisualStyles = false;
        _grid.DefaultCellStyle.BackColor = Color.FromArgb(28, 32, 38);
        _grid.DefaultCellStyle.ForeColor = Color.White;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 90, 120);
        _grid.DefaultCellStyle.SelectionForeColor = Color.White;
        _grid.GridColor = Color.FromArgb(48, 54, 62);

        _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Main", HeaderText = "Main", FillWeight = 42 });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Widget", HeaderText = "Widget", FillWeight = 52 });
        _grid.Columns.Add("Component", "Component");
        _grid.Columns.Add("Sensor", "Sensor");
        _grid.Columns.Add("Value", "Temperature");
        _grid.Columns.Add("Key", "Key");
        _grid.Columns["Key"]!.Visible = false;

        foreach (DataGridViewColumn column in _grid.Columns)
        {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        _grid.CellContentClick += (_, e) =>
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _grid.CellValueChanged += GridCellValueChanged;
    }

    private void UpdateDashboard(IReadOnlyList<SensorReading> readings, SensorHistoryStore history)
    {
        var selectedReadings = readings
            .Where(reading => _mainSensorKeys.Contains(SensorReadingIdentity.CreateKey(reading)))
            .ToArray();

        _dashboardPanel.SuspendLayout();

        foreach (var key in _tiles.Keys.Except(selectedReadings.Select(SensorReadingIdentity.CreateKey)).ToArray())
        {
            var tile = _tiles[key];
            _dashboardPanel.Controls.Remove(tile);
            tile.Dispose();
            _tiles.Remove(key);
        }

        foreach (var reading in selectedReadings)
        {
            var key = SensorReadingIdentity.CreateKey(reading);
            if (!_tiles.TryGetValue(key, out var tile))
            {
                tile = new TemperatureTileControl { Size = new Size(186, 94) };
                _tiles.Add(key, tile);
                _dashboardPanel.Controls.Add(tile);
            }

            tile.SetReading(reading, history.GetSamples(key));
        }

        _dashboardPanel.ResumeLayout();
    }

    private void UpdateGrid(IReadOnlyList<SensorReading> readings)
    {
        var firstDisplayedRowIndex = _grid.FirstDisplayedScrollingRowIndex >= 0
            ? _grid.FirstDisplayedScrollingRowIndex
            : -1;
        var selectedKey = _grid.CurrentRow?.Cells["Key"].Value as string;

        _isUpdatingGrid = true;
        _grid.Rows.Clear();

        foreach (var reading in readings)
        {
            var key = SensorReadingIdentity.CreateKey(reading);
            _grid.Rows.Add(
                _mainSensorKeys.Contains(key),
                _widgetSensorKeys.Contains(key),
                SensorReadingIdentity.CreateTitle(reading),
                SensorReadingIdentity.CreateSubtitle(reading),
                FormatValue(reading),
                key);
        }

        _isUpdatingGrid = false;

        RestoreGridPosition(firstDisplayedRowIndex, selectedKey);
    }

    private void RestoreGridPosition(int firstDisplayedRowIndex, string? selectedKey)
    {
        if (_grid.Rows.Count == 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(selectedKey))
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (string.Equals(
                    row.Cells["Key"].Value as string,
                    selectedKey,
                    StringComparison.OrdinalIgnoreCase))
                {
                    _grid.CurrentCell = row.Cells["Sensor"];
                    row.Selected = true;
                    break;
                }
            }
        }

        if (firstDisplayedRowIndex < 0)
        {
            return;
        }

        _grid.FirstDisplayedScrollingRowIndex = Math.Min(
            firstDisplayedRowIndex,
            _grid.Rows.Count - 1);
    }

    private void GridCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_isUpdatingGrid || e.RowIndex < 0)
        {
            return;
        }

        var columnName = _grid.Columns[e.ColumnIndex].Name;
        if (columnName is not ("Main" or "Widget"))
        {
            return;
        }

        var row = _grid.Rows[e.RowIndex];
        var key = Convert.ToString(row.Cells["Key"].Value);
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var isEnabled = Convert.ToBoolean(row.Cells[e.ColumnIndex].Value);
        if (columnName == "Main")
        {
            _setMainSensor(key, isEnabled);
            return;
        }

        _setWidgetSensor(key, isEnabled);
    }

    private static string FormatValue(SensorReading reading)
    {
        return reading.Value is { } value
            ? $"{value:0.#} {reading.Unit}".TrimEnd()
            : "N/A";
    }
}
