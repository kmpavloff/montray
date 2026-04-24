using Montray.Core;
using Montray.Hardware;

namespace Montray;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly HardwareMonitorService _hardwareMonitor;
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private DetailsForm? _detailsForm;
    private IReadOnlyList<SensorReading> _lastReadings = Array.Empty<SensorReading>();

    public TrayApplicationContext(HardwareMonitorService hardwareMonitor)
    {
        _hardwareMonitor = hardwareMonitor;

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
            _detailsForm?.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Show details", null, (_, _) => ShowDetails());
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
            _detailsForm?.SetReadings(_lastReadings);
        }
        catch (Exception ex)
        {
            _notifyIcon.Text = TrayTooltipFormatter.FormatSummary(Array.Empty<SensorReading>());
            _detailsForm?.ShowError(ex.Message);
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

    private void Exit()
    {
        _timer.Stop();
        _notifyIcon.Visible = false;
        ExitThread();
    }
}
