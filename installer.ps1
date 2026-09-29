$ErrorActionPreference = "Stop"
$dist = Join-Path $PSScriptRoot "dist"
$sortie = Join-Path $env:TEMP "MesWidgets-publication"

dotnet publish (Join-Path $PSScriptRoot "MesWidgets.csproj") -c Release -o $sortie
if ($LASTEXITCODE -ne 0) { throw "La compilation a échoué." }

New-Item -ItemType Directory -Force $dist | Out-Null
$setup = Join-Path $dist "MesWidgets-Setup.exe"
Copy-Item (Join-Path $sortie "MesWidgets.exe") $setup -Force
Remove-Item $sortie -Recurse -Force

(Start-Process $setup -ArgumentList "--silencieux" -PassThru).WaitForExit()
Write-Host ("Installateur : {0} ({1:N0} Mo)" -f $setup, ((Get-Item $setup).Length / 1MB))
Write-Host "Mes Widgets installé et lancé."
