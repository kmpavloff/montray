param(
    [switch]$PauseOnExit
)

$ErrorActionPreference = 'Stop'

$serviceName = 'MontraySensorService'
$displayName = 'montray sensor service'
$logDirectory = Join-Path $env:LOCALAPPDATA 'montray\logs'
$logPath = Join-Path $logDirectory 'uninstall-service.log'

New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
Start-Transcript -Path $logPath -Append | Out-Null

$exitCode = 0
try {
    if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Run this script from an elevated PowerShell session.'
    }

    $existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if ($null -eq $existingService) {
        Write-Host "$displayName is not installed."
        Write-Host "Log: $logPath"
        return
    }

    if ($existingService.Status -ne 'Stopped') {
        Stop-Service -Name $serviceName -Force
        $existingService.WaitForStatus('Stopped', '00:00:15')
    }

    sc.exe delete $serviceName | Out-Null
    Write-Host "$displayName was removed."
    Write-Host "Log: $logPath"
} catch {
    $exitCode = 1
    Write-Error $_
    Write-Host "Log: $logPath"
} finally {
    Stop-Transcript | Out-Null

    if ($PauseOnExit) {
        Read-Host 'Press Enter to close this window'
    }
}

exit $exitCode
