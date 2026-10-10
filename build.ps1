$ErrorActionPreference = 'Stop'

$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $compiler)) {
    throw "The .NET Framework C# compiler was not found: $compiler"
}

& $compiler /nologo /target:exe /platform:anycpu /optimize+ /r:System.Web.Extensions.dll `
    /out:easycmd.exe AssemblyInfo.cs EasyCmd.cs
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Built $((Resolve-Path .\easycmd.exe).Path)"
