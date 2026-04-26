namespace Montray;

internal sealed class SettingsForm : Form
{
    private readonly CheckBox _alwaysOnTopCheckBox;
    private readonly CheckBox _sparklineCheckBox;
    private readonly NumericUpDown _opacityInput;
    private readonly NumericUpDown _warmInput;
    private readonly NumericUpDown _hotInput;
    private readonly NumericUpDown _criticalInput;

    public SettingsForm(
        bool widgetAlwaysOnTop,
        double widgetOpacity,
        bool widgetShowSparkline,
        TemperatureThresholds thresholds)
    {
        Text = "montray settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(360, 260);
        BackColor = Color.FromArgb(24, 28, 33);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9f);

        var normalized = thresholds.Normalize();

        _alwaysOnTopCheckBox = new CheckBox
        {
            Text = "Widget always on top",
            Checked = widgetAlwaysOnTop,
            AutoSize = true,
            Location = new Point(18, 18)
        };

        _sparklineCheckBox = new CheckBox
        {
            Text = "Show widget sparklines",
            Checked = widgetShowSparkline,
            AutoSize = true,
            Location = new Point(18, 46)
        };

        _opacityInput = CreateInput(widgetOpacity * 100, 35, 100, new Point(220, 76));
        _warmInput = CreateInput(normalized.Warm, 1, 150, new Point(220, 116));
        _hotInput = CreateInput(normalized.Hot, 2, 160, new Point(220, 146));
        _criticalInput = CreateInput(normalized.Critical, 3, 170, new Point(220, 176));

        Controls.Add(_alwaysOnTopCheckBox);
        Controls.Add(_sparklineCheckBox);
        AddLabel("Widget opacity, %", new Point(18, 79));
        AddLabel("Warm threshold, °C", new Point(18, 119));
        AddLabel("Hot threshold, °C", new Point(18, 149));
        AddLabel("Critical threshold, °C", new Point(18, 179));
        Controls.Add(_opacityInput);
        Controls.Add(_warmInput);
        Controls.Add(_hotInput);
        Controls.Add(_criticalInput);

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Size = new Size(82, 30),
            Location = new Point(ClientSize.Width - 188, ClientSize.Height - 44)
        };
        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(82, 30),
            Location = new Point(ClientSize.Width - 100, ClientSize.Height - 44)
        };

        Controls.Add(okButton);
        Controls.Add(cancelButton);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    public bool WidgetAlwaysOnTop => _alwaysOnTopCheckBox.Checked;

    public double WidgetOpacity => (double)_opacityInput.Value / 100.0;

    public bool WidgetShowSparkline => _sparklineCheckBox.Checked;

    public TemperatureThresholds Thresholds => new(
        (float)_warmInput.Value,
        (float)_hotInput.Value,
        (float)_criticalInput.Value);

    private void AddLabel(string text, Point location)
    {
        Controls.Add(new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = Color.FromArgb(190, 200, 212),
            Location = location
        });
    }

    private static NumericUpDown CreateInput(double value, decimal minimum, decimal maximum, Point location)
    {
        return new NumericUpDown
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = Math.Clamp((decimal)value, minimum, maximum),
            DecimalPlaces = 0,
            Increment = 1,
            Size = new Size(88, 25),
            Location = location
        };
    }
}
