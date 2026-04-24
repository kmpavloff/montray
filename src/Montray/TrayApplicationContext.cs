using Montray.Core;
using Montray.Hardware;

namespace Montray;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly HardwareMonitorService _hardwareMonitor;
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly ToolStripMenuItem _toggleWidgetMenuItem;
    private Icon? _temperatureIcon;
    private DetailsForm? _detailsForm;
    private FloatingWidgetForm? _floatingWidget;
    private IReadOnlyList<SensorReading> _lastReadings = Array.Empty<SensorReading>();

    public TrayApplicationContext(HardwareMonitorService hardwareMonitor)
    {
        _hardwareMonitor = hardwareMonitor;

        _toggleWidgetMenuItem = new ToolStripMenuItem("Show widget", null, (_, _) => ToggleWidget());

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "montray | CPU N/A | GPU N/A",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };
        _notifyIcon.DoubleClick += (_, _) => ShowDetails();

        _timer = new System.Windows.Forms.Timer
        {
            Interval = 2000
        };
        _timer.Tick += (_, _) => UpdateReadings();
        _timer.Start();

        UpdateReadings();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _temperatureIcon?.Dispose();
            _detailsForm?.Dispose();
            _floatingWidget?.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Show details", null, (_, _) => ShowDetails());
        menu.Items.Add(_toggleWidgetMenuItem);
        menu.Items.Add("Refresh sensors", null, (_, _) => RefreshSensors());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Exit());
        return menu;
    }

    private void UpdateReadings()
    {
        try
        {
            _lastReadings = _hardwareMonitor.GetReadings();
            _notifyIcon.Text = TrayTooltipFormatter.FormatSummary(_lastReadings);
            UpdateTrayIcon();
            _detailsForm?.SetReadings(_lastReadings);
            _floatingWidget?.SetReadings(_lastReadings);
        }
        catch (Exception ex)
        {
            _notifyIcon.Text = TrayTooltipFormatter.FormatSummary(Array.Empty<SensorReading>());
            UpdateTrayIcon();
            _detailsForm?.ShowError(ex.Message);
            _floatingWidget?.ShowError(ex.Message);
        }
    }

    private void RefreshSensors()
    {
        try
        {
            _hardwareMonitor.Refresh();
        }
        catch (Exception ex)
        {
            _detailsForm?.ShowError(ex.Message);
        }

        UpdateReadings();
    }

    private void ShowDetails()
    {
        if (_detailsForm is null || _detailsForm.IsDisposed)
        {
            _detailsForm = new DetailsForm();
            _detailsForm.FormClosed += (_, _) => _detailsForm = null;
        }

        _detailsForm.SetReadings(_lastReadings);
        _detailsForm.Show();
        _detailsForm.Activate();
    }

    private void ToggleWidget()
    {
        if (_floatingWidget is not null && !_floatingWidget.IsDisposed && _floatingWidget.Visible)
        {
            _floatingWidget.Hide();
            _toggleWidgetMenuItem.Text = "Show widget";
            return;
        }

        if (_floatingWidget is null || _floatingWidget.IsDisposed)
        {
            _floatingWidget = new FloatingWidgetForm();
        }

        _floatingWidget.SetReadings(_lastReadings);
        _floatingWidget.Show();
        _floatingWidget.Activate();
        _toggleWidgetMenuItem.Text = "Hide widget";
    }

    private void UpdateTrayIcon()
    {
        var previousIcon = _temperatureIcon;
        _temperatureIcon = TrayTemperatureIconRenderer.Render(_lastReadings);
        _notifyIcon.Icon = _temperatureIcon;
        previousIcon?.Dispose();
    }

    private void Exit()
    {
        _timer.Stop();
        _notifyIcon.Visible = false;
        ExitThread();
    }
}
