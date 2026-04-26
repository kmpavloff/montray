using Montray.Core;
using Montray.Hardware;
using Montray.ServiceManagement;

namespace Montray;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly HardwareMonitorService _hardwareMonitor;
    private readonly SensorPipeClient _sensorPipeClient = new();
    private readonly SensorServiceManager _sensorServiceManager = new();
    private readonly NotifyIcon _notifyIcon;
    private readonly SensorHistoryStore _history = new();
    private readonly UserSensorSelectionStore _selectionStore = new();
    private readonly SemaphoreSlim _hardwareAccess = new(1, 1);
    private readonly System.Windows.Forms.Timer _timer;
    private readonly ToolStripMenuItem _toggleWidgetMenuItem;
    private readonly ToolStripMenuItem _serviceStatusMenuItem;
    private readonly ToolStripMenuItem _installServiceMenuItem;
    private readonly ToolStripMenuItem _uninstallServiceMenuItem;
    private readonly ToolStripMenuItem _refreshServiceStatusMenuItem;
    private readonly HashSet<string> _mainSensorKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _widgetSensorKeys = new(StringComparer.OrdinalIgnoreCase);
    private Icon? _temperatureIcon;
    private DetailsForm? _detailsForm;
    private FloatingWidgetForm? _floatingWidget;
    private IReadOnlyList<SensorReading> _lastReadings = Array.Empty<SensorReading>();
    private bool _mainSensorsInitialized;
    private bool _isPolling;
    private bool _lastReadUsedService;
    private string? _serviceOperationText;
    private DateTime _serviceOperationPollUntil;

    public TrayApplicationContext(HardwareMonitorService hardwareMonitor)
    {
        _hardwareMonitor = hardwareMonitor;
        LoadUserSelection();

        _toggleWidgetMenuItem = new ToolStripMenuItem("Show widget", null, (_, _) => ToggleWidget());
        _serviceStatusMenuItem = new ToolStripMenuItem("Sensor service: checking")
        {
            Enabled = false
        };
        _installServiceMenuItem = new ToolStripMenuItem("Install sensor service", null, (_, _) => InstallSensorService());
        _uninstallServiceMenuItem = new ToolStripMenuItem("Uninstall sensor service", null, (_, _) => UninstallSensorService());
        _refreshServiceStatusMenuItem = new ToolStripMenuItem("Refresh service status", null, async (_, _) => await RefreshServiceStatusAsync());

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
        _timer.Tick += async (_, _) => await UpdateReadingsAsync();
        _timer.Start();

        _ = UpdateReadingsAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _hardwareAccess.Dispose();
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
        menu.Opening += (_, _) => UpdateServiceMenuItems();
        menu.Items.Add("Show details", null, (_, _) => ShowDetails());
        menu.Items.Add(_toggleWidgetMenuItem);
        menu.Items.Add("Refresh sensors", null, async (_, _) => await RefreshSensorsAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_serviceStatusMenuItem);
        menu.Items.Add(_installServiceMenuItem);
        menu.Items.Add(_uninstallServiceMenuItem);
        menu.Items.Add(_refreshServiceStatusMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Exit());
        UpdateServiceMenuItems();
        return menu;
    }

    private async Task UpdateReadingsAsync()
    {
        if (_isPolling)
        {
            return;
        }

        _isPolling = true;
        try
        {
            var readings = await ReadHardwareAsync();
            _lastReadings = readings;
            _history.AddReadings(_lastReadings);
            EnsureDefaultMainSensors();
            _notifyIcon.Text = TrayTooltipFormatter.FormatSummary(_lastReadings);
            UpdateTrayIcon();
            _detailsForm?.SetReadings(_lastReadings, _mainSensorKeys, _widgetSensorKeys, _history);
            _floatingWidget?.SetReadings(_lastReadings, _widgetSensorKeys, _history);
        }
        catch (Exception ex)
        {
            _notifyIcon.Text = TrayTooltipFormatter.FormatSummary(Array.Empty<SensorReading>());
            UpdateTrayIcon();
            _detailsForm?.ShowError(ex.Message);
            _floatingWidget?.ShowError(ex.Message);
        }
        finally
        {
            UpdateServiceMenuItems();
            _isPolling = false;
        }
    }

    private async Task RefreshSensorsAsync()
    {
        try
        {
            await RefreshHardwareAsync();
        }
        catch (Exception ex)
        {
            _detailsForm?.ShowError(ex.Message);
        }

        await UpdateReadingsAsync();
    }

    private async Task<IReadOnlyList<SensorReading>> ReadHardwareAsync()
    {
        var serviceReadings = await _sensorPipeClient.GetReadingsAsync(TimeSpan.FromMilliseconds(500));
        if (serviceReadings is not null)
        {
            _lastReadUsedService = true;
            return serviceReadings;
        }

        _lastReadUsedService = false;

        await _hardwareAccess.WaitAsync();
        try
        {
            return await Task.Run(_hardwareMonitor.GetReadings);
        }
        finally
        {
            _hardwareAccess.Release();
        }
    }

    private async Task RefreshHardwareAsync()
    {
        var serviceReadings = await _sensorPipeClient.RefreshAsync(TimeSpan.FromMilliseconds(800));
        if (serviceReadings is not null)
        {
            _lastReadUsedService = true;
            _lastReadings = serviceReadings;
            return;
        }

        _lastReadUsedService = false;

        await _hardwareAccess.WaitAsync();
        try
        {
            await Task.Run(_hardwareMonitor.Refresh);
        }
        finally
        {
            _hardwareAccess.Release();
        }
    }

    private void InstallSensorService()
    {
        var result = MessageBox.Show(
            "Windows will show a UAC prompt to install the sensor service. The elevated PowerShell window will stay open with the result.",
            "montray",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Information);

        if (result != DialogResult.OK)
        {
            return;
        }

        if (!_sensorServiceManager.TryLaunchInstall(out var errorMessage))
        {
            MessageBox.Show(errorMessage, "montray", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        TrackServiceOperation("installing");
    }

    private void UninstallSensorService()
    {
        var result = MessageBox.Show(
            "Windows will show a UAC prompt to remove the sensor service. The elevated PowerShell window will stay open with the result.",
            "montray",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning);

        if (result != DialogResult.OK)
        {
            return;
        }

        if (!_sensorServiceManager.TryLaunchUninstall(out var errorMessage))
        {
            MessageBox.Show(errorMessage, "montray", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        TrackServiceOperation("removing");
    }

    private void UpdateServiceMenuItems()
    {
        var state = _sensorServiceManager.GetState();
        if (_serviceOperationText == "installing" && state == SensorServiceState.Running)
        {
            _serviceOperationText = null;
        }
        else if (_serviceOperationText == "removing" && state == SensorServiceState.NotInstalled)
        {
            _serviceOperationText = null;
        }

        _serviceStatusMenuItem.Text = $"Sensor service: {FormatServiceState(state)}";
        _installServiceMenuItem.Enabled = state is SensorServiceState.NotInstalled or SensorServiceState.Stopped;
        _uninstallServiceMenuItem.Enabled = state is not SensorServiceState.NotInstalled;

        if (_serviceOperationText is not null)
        {
            if (DateTime.UtcNow <= _serviceOperationPollUntil)
            {
                _serviceStatusMenuItem.Text += $" ({_serviceOperationText})";
            }
            else
            {
                _serviceOperationText = null;
            }
        }

        if (_lastReadUsedService && state == SensorServiceState.Running)
        {
            _serviceStatusMenuItem.Text += " (used)";
        }
    }

    private async Task RefreshServiceStatusAsync()
    {
        _serviceOperationText = null;
        UpdateServiceMenuItems();
        await UpdateReadingsAsync();
    }

    private void TrackServiceOperation(string operationText)
    {
        _serviceOperationText = operationText;
        _serviceOperationPollUntil = DateTime.UtcNow.AddMinutes(2);
        UpdateServiceMenuItems();
        _notifyIcon.ShowBalloonTip(
            4000,
            "montray",
            "Service command started. Check the elevated PowerShell window for the result.",
            ToolTipIcon.Info);
    }

    private static string FormatServiceState(SensorServiceState state)
    {
        return state switch
        {
            SensorServiceState.NotInstalled => "not installed",
            SensorServiceState.Stopped => "stopped",
            SensorServiceState.StartPending => "starting",
            SensorServiceState.Running => "running",
            SensorServiceState.StopPending => "stopping",
            _ => "unknown"
        };
    }

    private void ShowDetails()
    {
        if (_detailsForm is null || _detailsForm.IsDisposed)
        {
            _detailsForm = new DetailsForm(SetMainSensor, SetWidgetSensor);
            _detailsForm.FormClosed += (_, _) => _detailsForm = null;
        }

        EnsureDefaultMainSensors();
        _detailsForm.SetReadings(_lastReadings, _mainSensorKeys, _widgetSensorKeys, _history);
        _detailsForm.Show();
        _detailsForm.Activate();
    }

    private void ToggleWidget()
    {
        if (_floatingWidget is not null && !_floatingWidget.IsDisposed && _floatingWidget.Visible)
        {
            HideWidget();
            return;
        }

        if (_floatingWidget is null || _floatingWidget.IsDisposed)
        {
            _floatingWidget = new FloatingWidgetForm(HideWidget, ShowDetails, Exit);
        }

        _floatingWidget.SetReadings(_lastReadings, _widgetSensorKeys, _history);
        _floatingWidget.Show();
        _floatingWidget.Activate();
        _toggleWidgetMenuItem.Text = "Hide widget";
    }

    private void HideWidget()
    {
        if (_floatingWidget is null || _floatingWidget.IsDisposed)
        {
            return;
        }

        _floatingWidget.Hide();
        _toggleWidgetMenuItem.Text = "Show widget";
    }

    private void UpdateTrayIcon()
    {
        var previousIcon = _temperatureIcon;
        _temperatureIcon = TrayTemperatureIconRenderer.Render(_lastReadings);
        _notifyIcon.Icon = _temperatureIcon;
        previousIcon?.Dispose();
    }

    private void SetMainSensor(string key, bool isEnabled)
    {
        _mainSensorsInitialized = true;
        SetSensor(_mainSensorKeys, key, isEnabled);
        SaveUserSelection();
        _detailsForm?.SetReadings(_lastReadings, _mainSensorKeys, _widgetSensorKeys, _history);
    }

    private void SetWidgetSensor(string key, bool isEnabled)
    {
        SetSensor(_widgetSensorKeys, key, isEnabled);
        SaveUserSelection();
        _detailsForm?.SetReadings(_lastReadings, _mainSensorKeys, _widgetSensorKeys, _history);
        _floatingWidget?.SetReadings(_lastReadings, _widgetSensorKeys, _history);
    }

    private static void SetSensor(HashSet<string> keys, string key, bool isEnabled)
    {
        if (isEnabled)
        {
            keys.Add(key);
            return;
        }

        keys.Remove(key);
    }

    private void EnsureDefaultMainSensors()
    {
        if (_mainSensorsInitialized)
        {
            return;
        }

        var defaultReadings = TemperatureReadingSelector.SelectTemperatureSummaries(_lastReadings)
            .Select(summary => summary.Reading)
            .OfType<SensorReading>()
            .ToArray();

        if (defaultReadings.Length == 0)
        {
            return;
        }

        foreach (var reading in defaultReadings)
        {
            _mainSensorKeys.Add(SensorReadingIdentity.CreateKey(reading));
        }

        _mainSensorsInitialized = true;
        SaveUserSelection();
    }

    private void LoadUserSelection()
    {
        var selection = _selectionStore.Load();
        _mainSensorKeys.UnionWith(selection.MainSensorKeys);
        _widgetSensorKeys.UnionWith(selection.WidgetSensorKeys);
        _mainSensorsInitialized = selection.HasSavedMainSensors;
    }

    private void SaveUserSelection()
    {
        try
        {
            _selectionStore.Save(_mainSensorKeys, _widgetSensorKeys);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void Exit()
    {
        _timer.Stop();
        _notifyIcon.Visible = false;
        ExitThread();
    }
}
