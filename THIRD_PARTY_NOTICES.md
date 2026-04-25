# Third-Party Notices

montray is licensed under the MIT License. Third-party dependencies keep their own licenses.

## LibreHardwareMonitorLib

- Package: `LibreHardwareMonitorLib`
- Version used by this project: `0.9.6`
- Project: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor
- License: Mozilla Public License 2.0
- License text: https://www.mozilla.org/MPL/2.0/

`LibreHardwareMonitorLib` provides hardware sensor access for CPU, GPU, memory, storage, motherboard, fan, voltage, load, and other readings. montray consumes normalized sensor readings from this library and does not modify the library source.

## PawnIO

- Project: https://pawnio.eu/

PawnIO is a separate low-level Windows kernel driver used by hardware monitoring software to access sensors that may not be available through normal user-mode APIs. It is not bundled with montray. Users may choose to install it separately when CPU or motherboard sensors are missing.

Because PawnIO runs as a kernel driver, install it only from a trusted source and only if you need the additional sensor access.

## .NET

montray is built with .NET and WinForms. Published self-contained builds include the .NET runtime components required to run the app.
