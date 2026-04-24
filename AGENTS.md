# Codex Handoff

This project is a planned Windows-only tray utility called montray. The user wants a small app that shows CPU and GPU temperature, and possibly other hardware parameters later.

## Decisions Already Made

- Use WinForms rather than Tauri, WPF, or WinUI 3 for the MVP.
- Use .NET 8 or newer.
- Use `LibreHardwareMonitorLib` for sensor access.
- Build and run from Windows PowerShell or Windows Terminal.
- Do not rely on WSL2 Linux `dotnet` for building/running WinForms.
- Editing code from WSL is acceptable; build and hardware validation happen manually on Windows.

## Environment Guidance

Recommended project location on Windows:

```text
C:\dev\montray
```

Build and run commands:

```powershell
dotnet restore .\src\Montray\Montray.csproj
dotnet build .\src\Montray\Montray.csproj
dotnet run --project .\src\Montray\Montray.csproj
```

Run unit tests:

```powershell
dotnet test .\tests\Montray.Core.Tests\Montray.Core.Tests.csproj
```

## Implementation Plan

1. Maintain a WinForms project targeting `net8.0-windows`.
2. Keep `LibreHardwareMonitorLib` as the sensor backend.
3. Use an application context that owns a `NotifyIcon`.
4. Poll sensors every 1-2 seconds.
5. Normalize readings into a small model.
6. Update the tray tooltip with CPU and GPU temperature when available.
7. Keep a tray menu with `Show details`, `Refresh sensors`, and `Exit`.
8. Keep a simple details form that lists detected sensors.
9. Handle missing sensors gracefully: show `N/A` rather than failing.
10. Only after the MVP works, consider dynamic tray icons, autostart, thresholds, and history graphs.

## Suggested Structure

- `src/Montray/Program.cs`: application entry point.
- `src/Montray/TrayApplicationContext.cs`: tray icon, menu, timer, and lifetime.
- `src/Montray/Hardware/HardwareMonitorService.cs`: LibreHardwareMonitor integration.
- `src/Montray.Core/SensorReading.cs`: normalized sensor reading model.
- `src/Montray.Core/TrayTooltipFormatter.cs`: tray tooltip formatting.
- `src/Montray/DetailsForm.cs`: simple readings window.

Keep LibreHardwareMonitor usage inside `HardwareMonitorService`. Forms and tray code should consume normalized readings, not raw hardware objects.

## Technical Notes

- WinForms `NotifyIcon` is the main reason for choosing WinForms.
- WinUI 3 was discussed and rejected for MVP because tray integration is less direct.
- Tauri was discussed and rejected for MVP because sensor access still needs native integration and adds unnecessary WebView/Rust/Node complexity.
- Dynamic tray icon text is a later feature; start with tooltip updates unless the user explicitly changes priority.
- Some hardware sensors may require elevated privileges or may not be exposed by the library. The app should degrade gracefully.

## User Preferences

- The app should be small and tray-first.
- Prioritize practical implementation over visual polish for the first version.
- Communicate in Russian unless the user switches language.
