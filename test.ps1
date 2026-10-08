$ErrorActionPreference = 'Stop'

& .\build.ps1

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
foreach ($expected in @('easycmd v0.1.8.0', 'Copyright (c) 2026 yydylab', 'https://github.com/yydylab/easycmd')) {
    if (-not $banner.Contains($expected)) {
        throw "Version banner does not contain '$expected'."
    }
}
Write-Host "PASS version banner"

$key = 'HKCU:\Software\Microsoft\Command Processor'
$legacyAutoRun = [string](Get-ItemProperty -Path $key -ErrorAction SilentlyContinue).AutoRun
& .\easycmd.exe install | Out-Null
$migratedAutoRun = [string](Get-ItemProperty -Path $key).AutoRun
if ($legacyAutoRun -match 'acmd\.exe' -and $migratedAutoRun -match 'acmd\.exe') {
    throw 'Install did not remove legacy ACMD macros.'
}
& .\easycmd.exe uninstall | Out-Null
$before = [string](Get-ItemProperty -Path $key -ErrorAction SilentlyContinue).AutoRun
& .\easycmd.exe install
$installed = [string](Get-ItemProperty -Path $key).AutoRun
foreach ($macro in @('doskey cc=', 'doskey ia=', 'doskey rp=', 'doskey tp=')) {
    if ($installed -notmatch [regex]::Escape($macro)) {
        throw "Install did not register $macro."
    }
}
& .\easycmd.exe uninstall
$after = [string](Get-ItemProperty -Path $key -ErrorAction SilentlyContinue).AutoRun
if ($after -cne $before) {
    throw 'Uninstall did not restore the prior AutoRun setting.'
}
Write-Host "PASS EasyCMD macro install and uninstall"
