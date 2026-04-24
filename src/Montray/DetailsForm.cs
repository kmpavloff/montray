using Montray.Core;

namespace Montray;

public sealed class DetailsForm : Form
{
    private readonly DataGridView _grid;
    private readonly Label _statusLabel;

    public DetailsForm()
    {
        Text = "montray details";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(760, 420);
        MinimumSize = new Size(560, 320);

        _statusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0)
        };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        _grid.Columns.Add("Category", "Category");
        _grid.Columns.Add("Hardware", "Hardware");
        _grid.Columns.Add("Sensor", "Sensor");
        _grid.Columns.Add("Value", "Value");

        Controls.Add(_grid);
        Controls.Add(_statusLabel);
    }

    public void SetReadings(IReadOnlyList<SensorReading> readings)
    {
        _grid.Rows.Clear();

        foreach (var reading in readings)
        {
            _grid.Rows.Add(
                reading.Category,
                reading.HardwareName,
                reading.SensorName,
                FormatValue(reading));
        }

        _statusLabel.Text = readings.Count == 0
            ? "No sensors detected."
            : $"Sensors: {readings.Count}";
    }

    public void ShowError(string message)
    {
        _statusLabel.Text = $"Sensor error: {message}";
    }

    private static string FormatValue(SensorReading reading)
    {
        return reading.Value is { } value
            ? $"{value:0.#} {reading.Unit}".TrimEnd()
            : "N/A";
    }
}
