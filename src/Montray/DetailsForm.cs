using Montray.Core;

namespace Montray;

public sealed class DetailsForm : Form
{
    private readonly DataGridView _grid;
    private readonly Label _statusLabel;

    public DetailsForm()
    {
        Text = "montray temperatures";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(560, 260);
        MinimumSize = new Size(460, 220);

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

        _grid.Columns.Add("Component", "Component");
        _grid.Columns.Add("Sensor", "Sensor");
        _grid.Columns.Add("Value", "Temperature");

        Controls.Add(_grid);
        Controls.Add(_statusLabel);
    }

    public void SetReadings(IReadOnlyList<SensorReading> readings)
    {
        _grid.Rows.Clear();

        var summaries = TemperatureReadingSelector.SelectTemperatureSummaries(readings);

        foreach (var summary in summaries)
        {
            _grid.Rows.Add(
                summary.Component,
                FormatSensor(summary.Reading),
                FormatValue(summary.Reading));
        }

        _statusLabel.Text = summaries.All(summary => summary.Reading is null)
            ? "No temperature sensors detected."
            : "Current temperatures";
    }

    public void ShowError(string message)
    {
        _statusLabel.Text = $"Sensor error: {message}";
    }

    private static string FormatSensor(SensorReading? reading)
    {
        return reading is null
            ? "N/A"
            : $"{reading.HardwareName} / {reading.SensorName}";
    }

    private static string FormatValue(SensorReading? reading)
    {
        return reading?.Value is { } value
            ? $"{value:0.#} {reading.Unit}".TrimEnd()
            : "N/A";
    }
}
