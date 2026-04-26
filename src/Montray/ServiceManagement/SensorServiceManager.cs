using System.Diagnostics;
using System.ServiceProcess;
using Montray.Core;

namespace Montray.ServiceManagement;

public sealed class SensorServiceManager
{
    public SensorServiceState GetState()
    {
        try
        {
            using var controller = new ServiceController(SensorServiceConstants.ServiceName);
            _ = controller.Status;

            return controller.Status switch
            {
                ServiceControllerStatus.Running => SensorServiceState.Running,
                ServiceControllerStatus.Stopped => SensorServiceState.Stopped,
                ServiceControllerStatus.StartPending => SensorServiceState.StartPending,
                ServiceControllerStatus.StopPending => SensorServiceState.StopPending,
                _ => SensorServiceState.Unknown
            };
        }
        catch (InvalidOperationException)
        {
            return SensorServiceState.NotInstalled;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return SensorServiceState.NotInstalled;
        }
    }

    public bool TryFindServiceExecutable(out string serviceExePath)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var sameDirectoryPath = Path.Combine(baseDirectory, "montray-service.exe");
        if (File.Exists(sameDirectoryPath))
        {
            serviceExePath = sameDirectoryPath;
            return true;
        }

        var repoRoot = FindRepositoryRoot(baseDirectory);
        if (repoRoot is not null)
        {
            var debugPath = Path.Combine(
                repoRoot,
                "src",
                "Montray.Service",
                "bin",
                "Debug",
                "net8.0-windows",
                "montray-service.exe");

            if (File.Exists(debugPath))
            {
                serviceExePath = debugPath;
                return true;
            }

            var releasePath = Path.Combine(
                repoRoot,
                "src",
                "Montray.Service",
                "bin",
                "Release",
                "net8.0-windows",
                "montray-service.exe");

            if (File.Exists(releasePath))
            {
                serviceExePath = releasePath;
                return true;
            }
        }

        serviceExePath = string.Empty;
        return false;
    }

    public bool TryLaunchInstall(out string errorMessage)
    {
        if (!TryFindServiceExecutable(out var serviceExePath))
        {
            errorMessage = "montray-service.exe was not found. Build Montray.Service first.";
            return false;
        }

        return TryLaunchScript("install-service.ps1", $"-ServiceExePath {QuotePowerShell(serviceExePath)} -PauseOnExit", out errorMessage);
    }

    public bool TryLaunchUninstall(out string errorMessage)
    {
        return TryLaunchScript("uninstall-service.ps1", "-PauseOnExit", out errorMessage);
    }

    private static bool TryLaunchScript(string scriptName, string scriptArguments, out string errorMessage)
    {
        var packagedScriptPath = Path.Combine(AppContext.BaseDirectory, "scripts", scriptName);
        var repoRoot = FindRepositoryRoot(AppContext.BaseDirectory);
        var repoScriptPath = repoRoot is null ? null : Path.Combine(repoRoot, "scripts", scriptName);
        var scriptPath = File.Exists(packagedScriptPath) ? packagedScriptPath : repoScriptPath;
        if (scriptPath is null || !File.Exists(scriptPath))
        {
            errorMessage = $"{scriptName} was not found.";
            return false;
        }

        var command = $"& {QuotePowerShell(scriptPath)}";
        if (!string.IsNullOrWhiteSpace(scriptArguments))
        {
            command += " " + scriptArguments;
        }

        command = $"try {{ {command}; exit $LASTEXITCODE }} catch {{ Write-Error $_; Read-Host 'Press Enter to close this window'; exit 1 }}";
        var arguments = $"-NoProfile -ExecutionPolicy Bypass -Command {QuoteCommandArgument(command)}";

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = arguments,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = true,
                Verb = "runas"
            });

            errorMessage = string.Empty;
            return true;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
        catch (InvalidOperationException ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static string QuotePowerShell(string value)
    {
        return "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }

    private static string QuoteCommandArgument(string value)
    {
        return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private static string? FindRepositoryRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Montray.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
