# Install

montray is a Windows-only tray utility.

## Download

Download the latest `montray-*-win-x64.zip` from GitHub Releases and extract it to a local folder.

The package includes `README.txt` with end-user setup and troubleshooting notes.

Run:

```powershell
.\montray.exe
```

For best sensor coverage, install the optional sensor service from the tray menu:

1. Start `montray.exe`.
2. Open the tray menu.
3. Choose `Install sensor service`.
4. Approve the UAC prompt.

After installation, the tray app keeps running as a normal user process and reads elevated sensor data from `montray sensor service`.

## Sensor Access

CPU and motherboard sensors may require elevated permissions and a low-level hardware access driver. If CPU temperature is missing:

1. Install `montray sensor service` from the tray menu.
2. Install PawnIO from a trusted source if your hardware still requires it.
3. Restart the service or reinstall it from the tray menu.

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
2. Start `montray.exe`.
3. Install or reinstall `montray sensor service` from the tray menu.
4. Open `Show details` or `Show widget` and check whether CPU/motherboard temperatures appear.

Security note: PawnIO runs in kernel mode. Install it only if you need sensors that are otherwise missing, and only from a source you trust.

## Current Limitations

- No installer yet.
- No autostart setting yet.
- No code signing yet, so Windows may show a warning for downloaded builds.
- Service install/uninstall currently uses elevated PowerShell scripts.
- The app currently targets Windows x64 releases.

## Windows Security Warnings

Current release builds are not code-signed. Windows may show warnings such as:

- `Unknown publisher` when running as administrator.
- Microsoft Defender SmartScreen warnings for downloaded files.

This does not by itself mean the file is malicious; it means Windows cannot verify a trusted publisher signature or reputation for the executable.

Recommended precautions:

1. Download releases only from the official GitHub Releases page for this repository.
2. Do not run builds received from third-party mirrors or chat attachments.
3. If Windows shows SmartScreen, use `More info` only after confirming the file came from the official release.

Future signed releases may reduce these warnings, but that requires a paid code-signing certificate or trusted signing service.
