# Changelog

## Unreleased

## v0.1.2

- Added optional Windows Service for elevated hardware sensor access.
- Added tray menu service status, install, uninstall, and refresh actions.
- Added named pipe IPC between the tray app and sensor service.
- Moved LibreHardwareMonitor integration into a shared hardware project.
- Added elevated PowerShell install/uninstall scripts with logs.
- Added a user-facing `README.txt` to release packages.
- Updated release packaging to include the tray app, sensor service, service scripts, and user guide.

## v0.1.1

- Added CI workflow for build and tests.
- Added release workflow for tagged Windows builds.
- Added install and development documentation.
- Added MIT license and third-party notices.
- Fixed release packaging so the custom ZIP contains only the executable, license files, and install instructions.
- Documented that GitHub's automatic source archives are separate from the app distribution ZIP.

## v0.1.0

- Initial Windows tray utility.
- Dynamic tray icon showing CPU and GPU temperatures.
- Floating temperature widget for CPU, GPU, RAM, and SSD.
- Details window with summarized current temperatures.
- LibreHardwareMonitor-based sensor backend.
