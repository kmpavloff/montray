# Development

## Requirements

- Windows 10 or Windows 11.
- .NET 8 SDK or newer.
- Windows PowerShell or Windows Terminal.

Build and hardware validation should run on Windows, not WSL Linux, because the app targets WinForms and Windows hardware sensors.

## Commands

Restore and build:

```powershell
dotnet restore .\src\Montray\Montray.csproj
dotnet build .\src\Montray\Montray.csproj
```

Run tests:

```powershell
dotnet test .\tests\Montray.Core.Tests\Montray.Core.Tests.csproj
```

Run the same project-wide checks used by CI:

```powershell
dotnet restore .\Montray.sln
dotnet build .\Montray.sln --configuration Release --no-restore
dotnet test .\Montray.sln --configuration Release --no-build
```

Run locally:

```powershell
dotnet run --project .\src\Montray\Montray.csproj
```

Publish a local release build:

```powershell
dotnet publish .\src\Montray\Montray.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output .\artifacts\publish `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true
```

## Project Structure

- `src/Montray`: WinForms tray app.
- `src/Montray.Core`: normalized sensor models and formatting/selection logic.
- `tests/Montray.Core.Tests`: unit tests for core behavior.
- `.github/workflows`: CI and release automation.

Keep LibreHardwareMonitor-specific code inside `src/Montray/Hardware/HardwareMonitorService.cs`. UI code should consume normalized `SensorReading` values.

## Releases

Releases are created from git tags:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

The release workflow publishes a self-contained `win-x64` zip and attaches it to a GitHub Release.
