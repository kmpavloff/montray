# montray

montray is a small Windows tray utility for monitoring CPU and GPU temperatures, with room for additional hardware metrics later.

## Current Direction

- Target platform: Windows.
- UI technology: WinForms on .NET 8 or newer.
- Primary behavior: run in the system tray and expose current sensor values without requiring a full window to stay open.
- Sensor backend: `LibreHardwareMonitorLib`.
- Build/run environment: use Windows PowerShell or Windows Terminal, not WSL2 Linux `dotnet`, because WinForms targets the Windows Desktop SDK and the app must be tested in a real Windows session.

## Current MVP

The initial implementation is a WinForms tray app targeting `net8.0-windows`.

It currently includes:

1. A tray-only startup flow through `TrayApplicationContext`.
2. Periodic hardware polling through `LibreHardwareMonitorLib`.
3. A tray tooltip with CPU and GPU temperatures when available.
4. A tray context menu with:
   - Show details
   - Refresh sensors
   - Exit
5. A details window listing detected sensor readings.
6. Graceful missing-sensor behavior through `N/A` in the tray tooltip.

The first version should prefer reliability over visual polish. Dynamic tray icons with rendered temperature numbers can be added after the basic polling and tooltip behavior works.

## Windows Commands

Build and run from PowerShell or Windows Terminal in the Windows copy of this project:

```powershell
dotnet restore .\src\Montray\Montray.csproj
dotnet build .\src\Montray\Montray.csproj
dotnet run --project .\src\Montray\Montray.csproj
```

Run unit tests:

```powershell
dotnet test .\tests\Montray.Core.Tests\Montray.Core.Tests.csproj
```

If the project is moved from WSL to Windows, prefer a path like:

```text
C:\dev\montray
```

Avoid building WinForms from the Linux side of WSL2. Editing files from WSL is fine, but build, run, tray testing, and hardware sensor validation should happen on Windows.

## Architecture Sketch

Keep the app split into small parts:

- `src/Montray/Hardware/HardwareMonitorService.cs`: owns LibreHardwareMonitor setup, updates sensors, and returns normalized readings.
- `src/Montray.Core/SensorReading.cs`: simple model for name, hardware type, sensor type, value, and unit.
- `src/Montray.Core/TrayTooltipFormatter.cs`: formats the tray tooltip and keeps it within the `NotifyIcon` text limit.
- `src/Montray/TrayApplicationContext.cs`: owns `NotifyIcon`, tray menu, timers, and application lifetime.
- `src/Montray/DetailsForm.cs`: WinForms window for detected sensors and current readings.
- `AppSettings`: later home for polling interval, selected sensors, startup behavior, and thresholds.

The UI should not call LibreHardwareMonitor directly. It should consume readings from `HardwareMonitorService`, which keeps sensor access replaceable.

## Open Decisions

- Tray display mode: tooltip only for MVP, or dynamic tray icon with rendered temperature text.
- Which value should be primary when multiple CPU/GPU temperature sensors exist.
- Whether the app should request administrator privileges or run best-effort without elevation.
- Whether to add autostart in the MVP or defer it.
- Whether to persist selected sensors and thresholds in the MVP.

## Later Features

- Dynamic tray icon showing CPU or GPU temperature.
- Alerts when temperature exceeds a configured threshold.
- Autostart on Windows login.
- Sensor selection UI.
- Basic history graph.
- Fan speed, load, clocks, power draw, and memory temperature where available.
