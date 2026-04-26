# montray

montray is a small Windows tray utility for monitoring hardware temperatures.

It is tray-first: the app runs quietly in the notification area, updates a dynamic tray icon, and can show a compact floating widget when needed. The tray app itself is intended to run without administrator rights.

## Features

- Dynamic tray icon with CPU/GPU temperature rows.
- Floating widget with CPU, GPU, RAM, and SSD temperatures.
- Details window with summarized current temperatures.
- Tray menu with details, widget toggle, refresh, and exit actions.
- Optional Windows Service for elevated sensor access without running the tray app as administrator.
- Sensor backend powered by `LibreHardwareMonitorLib`.
- Graceful missing-sensor behavior through `N/A`.

## Requirements

- Windows 10 or Windows 11.
- .NET 8 runtime for framework-dependent local runs.
- Published releases are planned as self-contained Windows x64 builds.

Some CPU and motherboard sensors require elevated access. The tray app can install the optional `montray sensor service` through its menu; UAC is required only for service install/uninstall.

## Install

See [docs/INSTALL.md](docs/INSTALL.md).

Release ZIP packages include a user-facing `README.txt` with first-run, service install/uninstall, and troubleshooting steps.

## Sensor Service

`montray.exe` first tries to read sensor data from the optional Windows Service. If the service is not installed or not reachable, it falls back to local non-elevated sensor reads and shows unavailable values as `N/A`.

The service is installed from the tray menu:

1. Run `montray.exe`.
2. Open the tray menu.
3. Choose `Install sensor service`.
4. Approve the UAC prompt.

The service can also be removed from the tray menu with `Uninstall sensor service`.

## License

montray is licensed under the [MIT License](LICENSE).

Third-party dependencies are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Development

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md).

Quick local commands:

```powershell
dotnet restore .\Montray.sln
dotnet build .\Montray.sln
dotnet test .\Montray.sln
dotnet run --project .\src\Montray\Montray.csproj
```

Build, run, tray testing, and hardware validation should happen on Windows, not WSL Linux.
For full CPU/motherboard sensor coverage without running the tray app as administrator, build the solution and use the tray menu item `Install sensor service`.

## Releases

Releases are created from tags named like `v0.1.0`.

The release workflow publishes a self-contained `win-x64` zip and attaches it to a GitHub Release.

The package contains:

- `montray.exe`
- `montray-service.exe`
- `scripts\install-service.ps1`
- `scripts\uninstall-service.ps1`
- `README.txt`
- license and third-party notice files

## Limitations

- No installer yet.
- No autostart setting yet.
- No code signing yet.
- Service install/uninstall currently uses elevated PowerShell scripts.
- Sensor availability depends on hardware, BIOS, drivers, permissions, and LibreHardwareMonitor support.
