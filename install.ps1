$ErrorActionPreference = 'Stop'

$Repository = 'yydylab/easycmd'
$InstallPath = Join-Path $env:WINDIR 'System32\easycmd.exe'
$ProfilePath = Join-Path $env:LOCALAPPDATA 'EasyCMD'
$BackupPath = Join-Path $ProfilePath 'backups'

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Invoke-ElevatedInstaller {
    $temporaryScript = Join-Path $env:TEMP 'easycmd-install.ps1'
    Copy-Item -LiteralPath $PSCommandPath -Destination $temporaryScript -Force
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$temporaryScript`""
    $process = Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList $arguments -Wait -PassThru
    exit $process.ExitCode
}

if (-not (Test-IsAdministrator)) {
    Invoke-ElevatedInstaller
}

try {
    Write-Host 'Fetching the latest EasyCMD release...'
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/releases/latest" `
        -Headers @{ 'User-Agent' = 'EasyCMD-Installer' }
    $asset = @($release.assets | Where-Object { $_.name -eq 'easycmd.exe' })[0]
    if ($null -eq $asset) {
        throw 'The latest release does not contain easycmd.exe.'
    }

    $expectedVersion = [Version]($release.tag_name -replace '^v', '')
    $downloadPath = Join-Path $env:TEMP ('easycmd-' + [Guid]::NewGuid().ToString('N') + '.exe')
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $downloadPath
    $downloadedVersion = [Reflection.AssemblyName]::GetAssemblyName($downloadPath).Version
    if ($downloadedVersion.Major -ne $expectedVersion.Major -or
        $downloadedVersion.Minor -ne $expectedVersion.Minor -or
        $downloadedVersion.Build -ne $expectedVersion.Build) {
        throw "Downloaded version $downloadedVersion does not match release $($release.tag_name)."
    }

    New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
    Get-Process easycmd -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Milliseconds 500
    if (Test-Path $InstallPath) {
        $installedVersion = [Reflection.AssemblyName]::GetAssemblyName($InstallPath).Version
        $backupFile = Join-Path $BackupPath ('easycmd-' + $installedVersion + '-' + (Get-Date -Format 'yyyyMMddHHmmss') + '.exe')
        Copy-Item -LiteralPath $InstallPath -Destination $backupFile -Force
    }

    Copy-Item -LiteralPath $downloadPath -Destination $InstallPath -Force
    $verifiedVersion = [Reflection.AssemblyName]::GetAssemblyName($InstallPath).Version
    if ($verifiedVersion -ne $downloadedVersion) {
        throw "Installed version verification failed. Expected $downloadedVersion, got $verifiedVersion."
    }

    & $InstallPath install
    if ($LASTEXITCODE -ne 0) {
        throw "EasyCMD installation returned exit code $LASTEXITCODE."
    }

    Write-Host "EasyCMD v$verifiedVersion installed successfully."
    Write-Host 'Open a new CMD window to start EasyCMD.'
}
finally {
    if ($downloadPath -and (Test-Path $downloadPath)) {
        Remove-Item -LiteralPath $downloadPath -Force
    }
}
