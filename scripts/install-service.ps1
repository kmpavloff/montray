param(
    [Parameter(Mandatory = $true)]
    [string]$ServiceExePath,

    [switch]$PauseOnExit
)

$ErrorActionPreference = 'Stop'

$serviceName = 'MontraySensorService'
$displayName = 'montray sensor service'
$description = 'Reads hardware sensors for the montray tray application.'
$logDirectory = Join-Path $env:LOCALAPPDATA 'montray\logs'
$logPath = Join-Path $logDirectory 'install-service.log'

New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
Start-Transcript -Path $logPath -Append | Out-Null

$exitCode = 0
try {
    if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Run this script from an elevated PowerShell session.'
    }

    $resolvedServiceExePath = (Resolve-Path -LiteralPath $ServiceExePath).Path
    $existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

    if ($null -eq $existingService) {
        New-Service `
            -Name $serviceName `
            -BinaryPathName "`"$resolvedServiceExePath`"" `
            -DisplayName $displayName `
            -StartupType Automatic | Out-Null
    } else {
        if ($existingService.Status -ne 'Stopped') {
            Stop-Service -Name $serviceName -Force
            $existingService.WaitForStatus('Stopped', '00:00:15')
        }

        sc.exe config $serviceName binPath= "`"$resolvedServiceExePath`"" start= auto | Out-Null
    }

    sc.exe description $serviceName "$description" | Out-Null

    Start-Service -Name $serviceName
    $runningService = Get-Service -Name $serviceName
    $runningService.WaitForStatus('Running', '00:00:15')

    Write-Host "$displayName is installed and running."
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
