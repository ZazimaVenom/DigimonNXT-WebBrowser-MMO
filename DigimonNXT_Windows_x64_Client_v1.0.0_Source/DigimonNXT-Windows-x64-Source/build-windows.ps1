$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install the .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0 and reopen PowerShell.' }
if (!(Test-Path 'GameAssets')) { throw 'GameAssets is missing. Extract the complete source ZIP before building.' }
dotnet restore DigimonNXT.Client.csproj --locked-mode
if ($LASTEXITCODE -ne 0) { throw 'Dependency restore failed.' }
dotnet publish DigimonNXT.Client.csproj -c Release -r win-x64 --self-contained true --no-restore -o dist/Windows-x64
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Write-Host 'Built: dist/Windows-x64/DigimonNXT.exe'
