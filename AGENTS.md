# Codex Handoff

This project is a Windows-only tray utility called montray. It shows hardware temperatures in the notification area and keeps the tray app small, practical, and tray-first.

## Current State

- WinForms tray app targeting `net8.0-windows`.
- Dynamic tray icon with CPU/GPU temperature rows.
- Floating widget with CPU, GPU, RAM, and SSD temperatures.
- Details window with summarized current temperatures.
- Tray menu with details, widget toggle, refresh, service management, and exit actions.
- Optional Windows Service for elevated sensor access without running the tray app as administrator.
- Sensor backend powered by `LibreHardwareMonitorLib`.
- Missing or unavailable sensors are displayed as `N/A`.

## Decisions Already Made

- Use WinForms rather than Tauri, WPF, WinUI 3, or a web UI for the desktop app.
- Use .NET 8 or newer.
- Use `LibreHardwareMonitorLib` for sensor access.
- Keep the tray app non-elevated by default.
- Use the optional Windows Service when elevated access is needed for better sensor coverage.
- Build, run, tray testing, service testing, and hardware validation should happen on Windows.
- Do not rely on WSL2 Linux `dotnet` for building/running WinForms.
- Editing code from WSL is acceptable, but Windows validation is required.

## Environment Guidance

Recommended project location on Windows:

```text
C:\dev\montray
```

Restore and build:

```powershell
dotnet restore .\Montray.sln
dotnet build .\Montray.sln
```

Run unit tests:

```powershell
dotnet test .\tests\Montray.Core.Tests\Montray.Core.Tests.csproj
```

Run project-wide checks similar to CI:

```powershell
dotnet restore .\Montray.sln
dotnet build .\Montray.sln --configuration Release --no-restore
dotnet test .\Montray.sln --configuration Release --no-build
```

Run locally:

```powershell
dotnet run --project .\src\Montray\Montray.csproj
```

Manual service commands:

```powershell
.\scripts\install-service.ps1 -ServiceExePath .\src\Montray.Service\bin\Debug\net8.0-windows\montray-service.exe
.\scripts\uninstall-service.ps1
```

## Project Structure

- `src/Montray`: WinForms tray app, forms, tray icon rendering, widget, user settings, and service-management UI glue.
- `src/Montray.Core`: normalized sensor models, tooltip formatting, temperature selection, named-pipe constants/client types.
- `src/Montray.Hardware`: `LibreHardwareMonitorLib` integration. Keep hardware-library-specific code here.
- `src/Montray.Service`: Windows Service host that reads sensors with elevated rights and exposes readings over a local named pipe.
- `tests/Montray.Core.Tests`: unit tests for core behavior.
- `docs`: user/developer documentation.
- `scripts`: service install/uninstall PowerShell scripts.
- `.github/workflows`: CI and release automation.

Important files:

- `src/Montray/Program.cs`: WinForms entry point.
- `src/Montray/TrayApplicationContext.cs`: tray icon, menu, polling, and app lifetime.
- `src/Montray/TrayTemperatureIconRenderer.cs`: dynamic tray icon rendering.
- `src/Montray/FloatingWidgetForm.cs`: compact always-available temperature widget.
- `src/Montray/DetailsForm.cs`: current readings/details window.
- `src/Montray/SettingsForm.cs`: user settings UI.
- `src/Montray/ServiceManagement/SensorServiceManager.cs`: service install/uninstall/status integration.
- `src/Montray.Hardware/HardwareMonitorService.cs`: sensor backend implementation.
- `src/Montray.Service/SensorServiceHost.cs`: named-pipe service endpoint.

## Architecture Notes

- UI code should consume normalized `SensorReading` values, not raw LibreHardwareMonitor objects.
- Keep LibreHardwareMonitor-specific code inside `src/Montray.Hardware`.
- The tray app should first try the optional service through the named-pipe client, then fall back to local non-elevated sensor reads when the service is missing or unavailable.
- The tray app must continue to work when the service is not installed.
- Service install/uninstall requires UAC and is launched through PowerShell scripts.
- Any hardware data can be unavailable depending on permissions, motherboard, BIOS, drivers, and LibreHardwareMonitor support.
- Favor graceful degradation and clear `N/A` values over exceptions or blocking startup.

## Release Notes

- Releases are created from tags named like `v0.1.0`.
- The release workflow publishes a self-contained `win-x64` zip and attaches it to a GitHub Release.
- The app distribution should include `montray.exe`, `montray-service.exe`, service scripts, user-facing README, license, and third-party notices.
- There is no installer, autostart setting, or code signing yet.

## Future Work

- Autostart option.
- Installer.
- Code signing.
- Threshold notifications.
- History graphs.
- Broader tests around UI-independent selection/formatting/service-client behavior.

## User Preferences

- The app should stay small and tray-first.
- Prioritize practical implementation over visual polish.
- Communicate in Russian unless the user switches language.
