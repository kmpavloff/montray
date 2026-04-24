# montray

montray is a small Windows tray utility for monitoring hardware temperatures.

It is tray-first: the app runs quietly in the notification area, updates a dynamic tray icon, and can show a compact floating widget when needed.

## Features

- Dynamic tray icon with CPU/GPU temperature rows.
- Floating widget with CPU, GPU, RAM, and SSD temperatures.
- Details window with summarized current temperatures.
- Tray menu with details, widget toggle, refresh, and exit actions.
- Sensor backend powered by `LibreHardwareMonitorLib`.
- Graceful missing-sensor behavior through `N/A`.

## Requirements

- Windows 10 or Windows 11.
- .NET 8 runtime for framework-dependent local runs.
- Published releases are planned as self-contained Windows x64 builds.

Some CPU and motherboard sensors require administrator privileges and low-level hardware access through PawnIO/LibreHardwareMonitor support.

## Install

See [docs/INSTALL.md](docs/INSTALL.md).

## Development

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md).

Quick local commands:

```powershell
dotnet restore .\src\Montray\Montray.csproj
dotnet build .\src\Montray\Montray.csproj
dotnet test .\tests\Montray.Core.Tests\Montray.Core.Tests.csproj
dotnet run --project .\src\Montray\Montray.csproj
```

Build, run, tray testing, and hardware validation should happen on Windows, not WSL Linux.

## Releases

Releases are created from tags named like `v0.1.0`.

The release workflow publishes a self-contained `win-x64` zip and attaches it to a GitHub Release.

## Limitations

- No installer yet.
- No autostart setting yet.
- No code signing yet.
- Sensor availability depends on hardware, BIOS, drivers, permissions, and LibreHardwareMonitor support.
