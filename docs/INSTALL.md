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
2. Install PawnIO from a trusted source.
3. Restart `montray` as administrator.

Available sensors depend on the hardware, BIOS, drivers, and LibreHardwareMonitor support.

## Installing PawnIO

PawnIO is a separate Windows kernel driver used by hardware monitoring tools for low-level sensor access. It is not bundled with montray.

Recommended options:

1. Install the official signed PawnIO edition from https://pawnio.eu/.
2. If available on your system, install through Windows Package Manager:

```powershell
winget install PawnIO
```

After installation:

1. Fully exit `montray`.
2. Start `montray.exe` again as administrator.
3. Open `Show details` or `Show widget` and check whether CPU/motherboard temperatures appear.

Security note: PawnIO runs in kernel mode. Install it only if you need sensors that are otherwise missing, and only from a source you trust.

## Current Limitations

- No installer yet.
- No autostart setting yet.
- No code signing yet, so Windows may show a warning for downloaded builds.
- The app currently targets Windows x64 releases.
