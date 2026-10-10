$ErrorActionPreference = 'Stop'

& .\build.ps1

$languageKey = 'HKCU:\Software\EasyCMD'
$originalLanguage = Get-ItemProperty -Path $languageKey -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Language -ErrorAction SilentlyContinue

$cases = @(
    @{ Input = @('ping', 'https://xxx.com/login'); Expected = 'ping xxx.com' },
    @{ Input = @('tracert', '-d', '-w', '1', 'https://123.com/admin'); Expected = 'tracert -d -w 1 123.com' },
    @{ Input = @('nslookup', 'https://abc.cn/path'); Expected = 'nslookup abc.cn' },
    @{ Input = @('pathping', 'https://example.com/trace'); Expected = 'pathping example.com' },
    @{ Input = @('ping', 't', 'example.com'); Expected = 'ping -t example.com' },
    @{ Input = @('tracert', 'dw', 'example.com'); Expected = 'tracert -d -w 1 example.com' },
    @{ Input = @('ipconfig', 'a'); Expected = 'ipconfig /all' },
    @{ Input = @('ipconfig-all'); Expected = 'ipconfig /all' },
    @{ Input = @('ipconfig', 'f'); Expected = 'ipconfig /flushdns' },
    @{ Input = @('ipconfig-flushdns'); Expected = 'ipconfig /flushdns' },
    @{ Input = @('curl', 'c'); Expected = 'curl cip.cc' },
    @{ Input = @('curl-cip'); Expected = 'curl cip.cc' },
    @{ Input = @('curl-ipinfo'); Expected = 'curl ipinfo.io' },
    @{ Input = @('curl-cip', '1.1.1.1'); Expected = 'curl cip.cc/1.1.1.1' },
    @{ Input = @('curl-ipinfo', '8.8.8.8'); Expected = 'curl ipinfo.io/8.8.8.8' },
    @{ Input = @('curl-cip', '999.1.1.1'); Expected = 'curl cip.cc 999.1.1.1' },
    @{ Input = @('route', 'p'); Expected = 'route print' },
    @{ Input = @('route', 'p', '4'); Expected = 'route print -4' },
    @{ Input = @('route-print'); Expected = 'route print' },
    @{ Input = @('route-print-4'); Expected = 'route print -4' },
    @{ Input = @('route-print-6'); Expected = 'route print -6' },
    @{ Input = @('route', 'a', '223.5.5.5', '32', '192.168.1.1'); Expected = 'route add 223.5.5.5 mask 255.255.255.255 192.168.1.1' },
    @{ Input = @('mstsc', '192.168.1.1'); Expected = 'mstsc /v:192.168.1.1:3389' },
    @{ Input = @('tcping', '192.168.1.200', '3389'); Expected = 'tcping 192.168.1.200 3389' },
    @{ Input = @('tcping', '192.168.1.200'); Expected = 'tcping 192.168.1.200 22' }
)

foreach ($case in $cases) {
    $actual = (& .\easycmd.exe normalize @($case.Input)).Trim()
    if ($actual -ne $case.Expected) {
        throw "Expected '$($case.Expected)', got '$actual'."
    }
    Write-Host "PASS $actual"
}

$banner = (& .\easycmd.exe -v) -join "`n"
foreach ($expected in @('easycmd v0.1.17.0', 'Copyright (c) 2026 yydylab', 'https://github.com/yydylab/easycmd')) {
    if (-not $banner.Contains($expected)) {
        throw "Version banner does not contain '$expected'."
    }
}
Write-Host "PASS version banner"

& .\easycmd.exe en | Out-Null
$help = (& .\easycmd.exe help) -join "`n"
foreach ($expected in @('Tab: complete an EasyCMD command', 'Up/Down: browse previously entered commands', 'easycmd update exits the shell', 'No third-party command-line extension')) {
    if (-not $help.Contains($expected)) {
        throw "Help output does not contain '$expected'."
    }
}
& .\easycmd.exe cn | Out-Null
$language = [string](Get-ItemProperty -Path 'HKCU:\Software\EasyCMD').Language
if ($language -ne 'cn') {
    throw 'easycmd cn did not save the Chinese help language.'
}
$chineseHelp = (& .\easycmd.exe -h) -join "`n"
foreach ($expected in @([string][char]0x7B80, [string][char]0x68C0, [string][char]0x5207)) {
    if (-not $chineseHelp.Contains($expected)) {
        throw 'Chinese help output is missing expected Chinese text.'
    }
}
& .\easycmd.exe en | Out-Null
$language = [string](Get-ItemProperty -Path 'HKCU:\Software\EasyCMD').Language
if ($language -ne 'en') {
    throw 'easycmd en did not save the English help language.'
}
Write-Host "PASS interactive help settings"

$source = Get-Content -Raw .\EasyCmd.cs
foreach ($expected in @('IsInteractiveUpdateCommand', 'if (updateScheduled)', 'taskkill /f /im easycmd.exe', 'Closing running EasyCMD sessions', 'set attempts=0', 'if %attempts% GEQ 30', 'copy /y', 'Version verification failed', 'update.log')) {
    if (-not $source.Contains($expected)) {
        throw "Interactive update source does not contain '$expected'."
    }
}
Write-Host "PASS interactive update handoff"

$source = Get-Content -Raw .\EasyCmd.cs
foreach ($expected in @('EasyCmdCommands', 'CompleteEasyCmdLine', 'ShowEasyCmdHelp', 'ShowEasyCmdCommands')) {
    if (-not $source.Contains($expected)) {
        throw "EasyCMD completion source does not contain '$expected'."
    }
}
Write-Host "PASS EasyCMD command completion"

$source = Get-Content -Raw .\EasyCmd.cs
foreach ($expected in @('ExecuteEasyCmdSubcommand', 'UseShellExecute = true', 'RunCmdBuiltin', 'Arguments = "/d /c " + line', 'InteractiveGuiCommands', 'IsInteractiveGuiCommand', 'Console.CancelKeyPress')) {
    if (-not $source.Contains($expected)) {
        throw "Interactive execution source does not contain '$expected'."
    }
}
Write-Host "PASS interactive native command execution"

$history = (& .\easycmd.exe history) -join "`n"
if ([string]::IsNullOrWhiteSpace($history)) {
    throw 'History command did not return a status message.'
}
Write-Host "PASS history command"

$key = 'HKCU:\Software\Microsoft\Command Processor'
$originalAutoRun = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue | Select-Object -ExpandProperty AutoRun -ErrorAction SilentlyContinue

try {
    $legacyAutoRun = [string]$originalAutoRun
    & .\easycmd.exe install | Out-Null
    $migratedAutoRun = [string](Get-ItemProperty -Path $key).AutoRun
    if ($legacyAutoRun -match 'acmd\.exe' -and $migratedAutoRun -match 'acmd\.exe') {
        throw 'Install did not remove legacy ACMD macros.'
    }

    foreach ($macro in @('doskey cc=', 'doskey ia=', 'doskey rp=', 'doskey tp=')) {
        if ($migratedAutoRun -notmatch [regex]::Escape($macro)) {
            throw "Install did not register $macro."
        }
    }
    if ($migratedAutoRun -notmatch [regex]::Escape('easycmd.exe" shell')) {
        throw 'Install did not start the self-contained EasyCMD interactive shell.'
    }
    & .\easycmd.exe uninstall | Out-Null
    Write-Host "PASS EasyCMD macro install and uninstall"
}
finally {
    if ($null -eq $originalAutoRun) {
        Remove-ItemProperty -Path $key -Name AutoRun -ErrorAction SilentlyContinue
    }
    else {
        Set-ItemProperty -Path $key -Name AutoRun -Value $originalAutoRun
    }

    if ($null -eq $originalLanguage) {
        Remove-ItemProperty -Path $languageKey -Name Language -ErrorAction SilentlyContinue
    }
    else {
        Set-ItemProperty -Path $languageKey -Name Language -Value $originalLanguage
    }
}
