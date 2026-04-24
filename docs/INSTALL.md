# Install

montray is a Windows-only tray utility.

## Download

Download the latest `montray-*-win-x64.zip` from GitHub Releases and extract it to a local folder.

Run:

```powershell
.\montray.exe
```

## Sensor Access

CPU and motherboard sensors may require elevated permissions and a low-level hardware access driver. If CPU temperature is missing:

1. Run `montray.exe` as administrator.
2. Install or run LibreHardwareMonitor/FanControl with PawnIO support.
3. Restart `montray`.

Available sensors depend on the hardware, BIOS, drivers, and LibreHardwareMonitor support.

## Current Limitations

- No installer yet.
- No autostart setting yet.
- No code signing yet, so Windows may show a warning for downloaded builds.
- The app currently targets Windows x64 releases.
